using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using UnityEngine.SceneManagement;
using MEC;

public class IapManager : Singleton<IapManager>, IStoreListener
{
    private static IStoreController   m_StoreController;        // The Unity Purchasing system.
    private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.

    private readonly Dictionary<string, string> _PriceLocals = new Dictionary<string, string> ();
    private readonly Dictionary<string, float>  PriceFloats  = new Dictionary<string, float> ();

    [Header ("Data")] [SerializeField] private IAPData[] _IapData;

    private string currency_symbol;

    // -----------------------------------------------------------------------------
    // Public events — UI subscribes to these to react to purchase outcomes
    // without coupling to IapManager internals.
    //
    //   OnPurchaseSuccess(id)                        - delivery complete (granted to player)
    //   OnPurchaseFailed(id, userMessage, rawReason) - purchase failed; show userMessage in UI
    //   OnRestoreCompleted(anyPurchasesRestored)     - RestorePurchases finished
    //   OnInitialized()                              - store finished initializing
    // -----------------------------------------------------------------------------

    public event Action<IapEnums.IapId>                                          OnPurchaseSuccess;
    public event Action<IapEnums.IapId, string, PurchaseFailureReason>           OnPurchaseFailed;
    public event Action<bool>                                                    OnRestoreCompleted;
    public event Action                                                          OnInitializedEvent;

    // -----------------------------------------------------------------------------
    // Init retry with backoff. If the store is unreachable at app start (e.g.,
    // no network), we retry after 5s, 15s, 60s before giving up.
    // -----------------------------------------------------------------------------

    private static readonly float[] _RetryDelaysSeconds = { 5f, 15f, 60f };
    private int _retryAttempt;

#if RECEIPT_VALIDATION_ENABLED
    private CrossPlatformValidator _validator;
#endif

    private void Start ()
    {
        if (m_StoreController == null)
            InitializePurchasing ();
    }

    public void InitializePurchasing ()
    {
        if (IsInitialized ())
        {
            Debug.Log ("[IAP] Already initialized.");
            return;
        }

        var builder = ConfigurationBuilder.Instance (StandardPurchasingModule.Instance ());

        for (int i = 0; i < _IapData.Length; i++)
        {
            ProductType productType = _IapData[i].TypeIap == IapEnums.TypeIap.NonConsumable
                ? ProductType.NonConsumable
                : ProductType.Consumable;

            builder.AddProduct (_IapData[i].IapId, productType);

            // Seed the price dictionary with the offline price so UI has
            // something to show before the store finishes initializing.
            if (!_PriceLocals.ContainsKey (_IapData[i].IapId))
            {
                _PriceLocals.Add (_IapData[i].IapId, _IapData[i].PriceOffline);
                PriceFloats.Add (_IapData[i].IapId, 0.1f);
            }
        }

        currency_symbol = "USD";

        // -----------------------------------------------------------------------------
        // Receipt validator: rejects forged purchases on jailbroken devices.
        // Requires Tangle classes generated via:
        //   Services > In-App Purchasing > Receipt Validation Obfuscator
        // After generating, add RECEIPT_VALIDATION_ENABLED to Player Settings >
        // Scripting Define Symbols to activate validation.
        // -----------------------------------------------------------------------------
#if RECEIPT_VALIDATION_ENABLED
        try
        {
            _validator = new CrossPlatformValidator (
                GooglePlayTangle.Data (),
                AppleTangle.Data (),
                Application.identifier);
        }
        catch (Exception e)
        {
            Debug.LogError ("[IAP] Failed to construct receipt validator: " + e);
            _validator = null;
        }
#else
        Debug.LogWarning ("[IAP] Receipt validation is DISABLED. " +
                          "To enable: run Services > In-App Purchasing > Receipt Validation Obfuscator, " +
                          "then add RECEIPT_VALIDATION_ENABLED to Player Settings > Scripting Define Symbols.");
#endif

        UnityPurchasing.Initialize (this, builder);
    }

    #region PRODUCT

    public void BuyProductWithID (string id)
    {
        #if UNITY_EDITOR
        // Editor short-circuit so designers can verify the post-purchase flow
        // without needing a sandbox account. Skips receipt validation since
        // there is no real receipt to validate.
        for (int i = 0; i < _IapData.Length; i++)
        {
            if (string.CompareOrdinal (_IapData[i].IapId, id) == 0)
            {
                DeliverPurchase (_IapData[i]);
                break;
            }
        }
        return;
        #endif

        BuyProductID (id);
    }

    #endregion

    private bool IsInitialized ()
    {
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    void BuyProductID (string productId)
    {
        if (!IsInitialized ())
        {
            Debug.Log ("[IAP] BuyProductID FAIL. Not initialized.");
            FirePurchaseFailed (productId, "Store is not ready. Please try again in a moment.", PurchaseFailureReason.PurchasingUnavailable);
            return;
        }

        Product product = m_StoreController.products.WithID (productId);

        if (product == null || !product.availableToPurchase)
        {
            Debug.Log ("[IAP] BuyProductID FAIL. Product not found or unavailable: " + productId);
            FirePurchaseFailed (productId, "This item is currently unavailable.", PurchaseFailureReason.ProductUnavailable);
            return;
        }

        Debug.Log ("[IAP] Purchasing asynchronously: " + productId);
        m_StoreController.InitiatePurchase (product);
    }

    // -----------------------------------------------------------------------------
    // Restore Purchases (iOS-only — Google auto-restores).
    // -----------------------------------------------------------------------------

    public void RestorePurchases ()
    {
        if (!IsInitialized ())
        {
            Debug.Log ("[IAP] RestorePurchases FAIL. Not initialized.");
            FireRestoreCompleted (false);
            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log ("[IAP] RestorePurchases started ...");
            var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions> ();
            apple.RestoreTransactions (result =>
            {
                Debug.Log ("[IAP] RestorePurchases callback. AnyRestored=" + result);
                FireRestoreCompleted (result);
            });
        }
        else
        {
            // Android: purchases are restored automatically on app launch by
            // Google Play, so explicit restore is a no-op.
            Debug.Log ("[IAP] RestorePurchases: not needed on " + Application.platform);
            FireRestoreCompleted (true);
        }
    }

    /// <summary>
    /// Returns true if the given NonConsumable product is owned by the player,
    /// according to the store (not PlayerPrefs). Safe to call before init —
    /// returns false until the store is ready.
    /// </summary>
    public bool IsProductOwned (IapEnums.IapId id)
    {
        if (!IsInitialized ()) return false;

        string sku = LookupSku (id);
        if (string.IsNullOrEmpty (sku)) return false;

        Product product = m_StoreController.products.WithID (sku);
        return product != null
            && product.definition.type == ProductType.NonConsumable
            && product.hasReceipt;
    }

    // -----------------------------------------------------------------------------
    // IStoreListener
    // -----------------------------------------------------------------------------

    public void OnInitialized (IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log ("[IAP] OnInitialized: PASS");

        m_StoreController        = controller;
        m_StoreExtensionProvider = extensions;
        _retryAttempt            = 0;

        _PriceLocals.Clear ();
        PriceFloats.Clear ();

        foreach (var pro in controller.products.all)
        {
            if (!_PriceLocals.ContainsKey (pro.definition.id))
            {
                _PriceLocals.Add (pro.definition.id, pro.metadata.localizedPriceString);
                PriceFloats.Add (pro.definition.id, (float) pro.metadata.localizedPrice);
                currency_symbol = pro.metadata.isoCurrencyCode;
            }
        }

        // Re-sync the Remove Ads NonConsumable flag from the store, in case
        // PlayerPrefs lost it (corrupted save, reinstall before restore, etc).
        if (IsProductOwned (IapEnums.IapId.RemoveAdsPack))
        {
            PlayerData.IsRemoveAds = true;
            if (AdsManager.Instance != null) AdsManager.Instance.RefreshRemoveAds ();
        }

        if (OnInitializedEvent != null) OnInitializedEvent ();
    }

    public void OnInitializeFailed (InitializationFailureReason error)
    {
        OnInitializeFailed (error, null);
    }

    public void OnInitializeFailed (InitializationFailureReason error, string message)
    {
        Debug.LogWarning ("[IAP] OnInitializeFailed: " + error + (message != null ? " - " + message : ""));

        if (_retryAttempt < _RetryDelaysSeconds.Length)
        {
            float delay = _RetryDelaysSeconds[_retryAttempt++];
            Debug.Log ("[IAP] Retrying init in " + delay + "s (attempt " + _retryAttempt + ")...");
            Timing.CallDelayed (delay, InitializePurchasing);
        }
        else
        {
            Debug.LogError ("[IAP] Init failed after " + _RetryDelaysSeconds.Length +
                            " retries. Store will be unavailable for this session.");
        }
    }

    public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs args)
    {
        string id = args.purchasedProduct.definition.id;
        Debug.Log ("[IAP] ProcessPurchase: " + id);

        // -----------------------------------------------------------------------------
        // Validate receipt. Forged receipts (from jailbroken devices) trigger
        // an IAPSecurityException and we refuse delivery.
        // -----------------------------------------------------------------------------
#if RECEIPT_VALIDATION_ENABLED
        if (_validator != null)
        {
            try
            {
                _validator.Validate (args.purchasedProduct.receipt);
                Debug.Log ("[IAP] Receipt validated for " + id);
            }
            catch (IAPSecurityException e)
            {
                Debug.LogError ("[IAP] Invalid receipt rejected for " + id + ": " + e);
                IapEnums.IapId enumId;
                if (TryResolveIapEnumId (id, out enumId))
                    FirePurchaseFailed (id, "Purchase could not be verified.", PurchaseFailureReason.SignatureInvalid);
                return PurchaseProcessingResult.Complete; // consume so it doesn't loop
            }
        }
#endif

        // Find the IAPData entry that matches this product ID and deliver.
        for (int i = 0; i < _IapData.Length; i++)
        {
            if (string.CompareOrdinal (_IapData[i].IapId, id) == 0)
            {
                DeliverPurchase (_IapData[i]);
                break;
            }
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed (Product product, PurchaseFailureReason failureReason)
    {
        string sku = product != null ? product.definition.storeSpecificId : "(unknown)";
        Debug.LogWarning ("[IAP] OnPurchaseFailed: " + sku + " reason=" + failureReason);

        FirePurchaseFailed (
            product != null ? product.definition.id : sku,
            MapFailureReasonToUserMessage (failureReason),
            failureReason);
    }

    // -----------------------------------------------------------------------------
    // Delivery — unified entry point for both real purchases and editor sims.
    // Routes by the IAPData.id enum so adding new products only requires
    // adding a switch case.
    // -----------------------------------------------------------------------------

    private void DeliverPurchase (IAPData data)
    {
        switch (data.id)
        {
            case IapEnums.IapId.RemoveAdsPack:
                OnBuyRemoveAdsCompleted (data);
                break;

            case IapEnums.IapId.SmallDiamondsPack:
            case IapEnums.IapId.MediumDiamondsPack:
            case IapEnums.IapId.BigDiamondsPack:
            case IapEnums.IapId.FreePack:
                OnBuyDiamondsCompleted (data);
                break;

            default:
                Debug.LogWarning ("[IAP] No delivery handler for " + data.id);
                break;
        }

        if (OnPurchaseSuccess != null) OnPurchaseSuccess (data.id);
    }

    private void OnBuyDiamondsCompleted (IAPData data)
    {
        PlayerData.Diamonds += data.Value;
        PlayerData.SaveDiamonds ();

        if (GameActionManager.Instance != null)
        {
            GameActionManager.Instance.InstanceFxDiamonds (
                Vector.Vector3Zero,
                UIGameManager.Instance.GetPositionHubDiamonds (),
                data.Value, () =>
                {
                    UIGameManager.Instance.FxShakeDiamonds ();
                    GameActionManager.Instance.PostActionEvent (ActionEnums.ActionID.RefreshUIDiamonds);
                });
        }

        // FIXME: This line auto-grants Remove Ads when ANY diamond pack is bought.
        // If that's an intentional bundled benefit, keep this and remove this
        // comment. If unintended, delete the two lines below. The current code
        // means buying the cheapest diamond pack also removes ads — which may
        // not match what's described in your store listings.
        if (!PlayerData.IsRemoveAds)
            RemoveAds ();
    }

    private void OnBuyRemoveAdsCompleted (IAPData data)
    {
        PlayerData.Diamonds += data.Value;
        PlayerData.SaveDiamonds ();
        PlayerData.IsRemoveAds = true;

        if (GameActionManager.Instance != null)
        {
            GameActionManager.Instance.InstanceFxDiamonds (
                Vector.Vector3Zero,
                UIGameManager.Instance.GetPositionHubDiamonds (),
                data.Value,
                () =>
                {
                    UIGameManager.Instance.FxShakeDiamonds ();
                    GameActionManager.Instance.PostActionEvent (ActionEnums.ActionID.RefreshUIDiamonds);
                });
        }

        if (AdsManager.Instance != null)
            AdsManager.Instance.RefreshRemoveAds ();
    }

    private void RemoveAds ()
    {
        PlayerData.IsRemoveAds = true;
        if (AdsManager.Instance != null)
            AdsManager.Instance.RefreshRemoveAds ();
    }

    // -----------------------------------------------------------------------------
    // Price helpers (used by shop UI to show localized prices).
    // -----------------------------------------------------------------------------

    public string ReturnThePrice (IapEnums.IapId id)
    {
        for (int i = 0; i < _IapData.Length; i++)
            if (_IapData[i].id == id && _PriceLocals.ContainsKey (_IapData[i].IapId))
                return _PriceLocals[_IapData[i].IapId];
        return "????";
    }

    public float ReturnFloatPrice (IapEnums.IapId id)
    {
        for (int i = 0; i < _IapData.Length; i++)
            if (_IapData[i].id == id && PriceFloats.ContainsKey (_IapData[i].IapId))
                return PriceFloats[_IapData[i].IapId];
        return 0.1f;
    }

    // -----------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Translates a PurchaseFailureReason into a short, player-readable string.
    /// Wire this through your existing toast/dialog UI in the OnPurchaseFailed
    /// event subscriber.
    /// </summary>
    private static string MapFailureReasonToUserMessage (PurchaseFailureReason reason)
    {
        switch (reason)
        {
            case PurchaseFailureReason.PurchasingUnavailable: return "Store is unavailable. Please try again later.";
            case PurchaseFailureReason.ExistingPurchasePending: return "Another purchase is already in progress.";
            case PurchaseFailureReason.ProductUnavailable:    return "This item is currently unavailable.";
            case PurchaseFailureReason.SignatureInvalid:      return "Purchase could not be verified.";
            case PurchaseFailureReason.UserCancelled:         return "Purchase cancelled.";
            case PurchaseFailureReason.PaymentDeclined:       return "Payment was declined.";
            case PurchaseFailureReason.DuplicateTransaction:  return "This purchase has already been processed.";
            case PurchaseFailureReason.Unknown:
            default:                                          return "Purchase failed. Please try again.";
        }
    }

    private string LookupSku (IapEnums.IapId id)
    {
        for (int i = 0; i < _IapData.Length; i++)
            if (_IapData[i].id == id) return _IapData[i].IapId;
        return null;
    }

    private bool TryResolveIapEnumId (string sku, out IapEnums.IapId result)
    {
        for (int i = 0; i < _IapData.Length; i++)
        {
            if (string.CompareOrdinal (_IapData[i].IapId, sku) == 0)
            {
                result = _IapData[i].id;
                return true;
            }
        }
        result = default (IapEnums.IapId);
        return false;
    }

    private void FirePurchaseFailed (string sku, string userMessage, PurchaseFailureReason reason)
    {
        IapEnums.IapId id;
        if (TryResolveIapEnumId (sku, out id) && OnPurchaseFailed != null)
            OnPurchaseFailed (id, userMessage, reason);
    }

    private void FireRestoreCompleted (bool anyRestored)
    {
        if (OnRestoreCompleted != null) OnRestoreCompleted (anyRestored);
    }
}
