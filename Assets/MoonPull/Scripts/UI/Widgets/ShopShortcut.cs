using MoonPull.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>The "+" beside the coin counter: opens the shop.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ShopShortcut : MonoBehaviour
    {
        private void Awake() => GetComponent<Button>().onClick.AddListener(GameEvents.RaiseShopRequested);
    }
}
