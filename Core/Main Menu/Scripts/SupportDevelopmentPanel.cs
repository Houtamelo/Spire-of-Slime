using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Main_Menu.Scripts
{
    public class SupportDevelopmentPanel : MonoBehaviour
    {
        private const string SubscribeStarLink = "https://subscribestar.adult/spire-of-slime-yonder";
        private const string BitCoinAddress = "bc1qcqa335hzlvss4y290cvytsk9pwzm3s3dk4k97j";
        private const string EthereumAddress = "0x69552668Af1BbeFBA354f1A6F7F2dF5aBE9A31bA";
        private const string MoneroAddress = "46qi8UhDZebeuvrXrMf64w1EtQsFsEhkc1VQFVXCgkq94Z1qWdAEKTZFogmT1B3HjxcGFRmQKAZ8H8iwmDQV2ufpD1JnuyX";
        private const string Email = "houtamelo@pm.me";
        private const string Discord = "https://discord.gg/Cacam7yuqR";

        [SerializeField, Required]
        private GameObject mainPanel;

        [SerializeField, Required]
        private Button closeButton, openButton;

        [SerializeField, Required, BoxGroup(GroupID = "Donations")]
        private Toggle donationsToggle;

        [SerializeField, Required, BoxGroup(GroupID = "Donations")]
        private GameObject donationsMenu;

        [SerializeField, Required, BoxGroup(GroupID = "Subscribe Star")]
        private Toggle subscribeStarToggle;

        [SerializeField, Required, BoxGroup(GroupID = "Subscribe Star")]
        private GameObject subscribeStarPanel;

        [SerializeField, Required, BoxGroup(GroupID = "Subscribe Star")]
        private Button copySubscribeStar, openSubscribeStar;

        [SerializeField, Required, BoxGroup(GroupID = "Crypto")]
        private Toggle cryptoToggle;

        [SerializeField, Required, BoxGroup(GroupID = "Crypto")]
        private GameObject cryptoPanel;

        [SerializeField, Required, BoxGroup(GroupID = "Crypto")]
        private Button copyBitcoin, copyEthereum, copyMonero;

        [SerializeField, Required, BoxGroup(GroupID = "Crypto")]
        private Button emailButtonInsideCrypto;

        [SerializeField, Required, BoxGroup(GroupID = "Crypto")]
        private Button discordButtonInsideCrypto;

        [SerializeField, Required, BoxGroup(GroupID = "Contribute")]
        private Toggle contributeToggle;

        [SerializeField, Required, BoxGroup(GroupID = "Contribute")]
        private GameObject contributePanel;

        [SerializeField, Required, BoxGroup(GroupID = "Contribute")]
        private Button emailButtonInsideContribute;

        [SerializeField, Required, BoxGroup(GroupID = "Contribute")]
        private Button discordButtonInsideContribute;

        [SerializeField, Required, BoxGroup(GroupID = "Feedback")]
        private Toggle feedbackToggle;

        [SerializeField, Required, BoxGroup(GroupID = "Feedback")]
        private GameObject feedbackPanel;
        
        [SerializeField, Required, BoxGroup(GroupID = "Feedback")]
        private Button emailButtonInsideFeedback;

        [SerializeField, Required, BoxGroup(GroupID = "Feedback")]
        private Button discordButtonInsideFeedback;

        [SerializeField, Required, BoxGroup(GroupID = "Buy")]
        private Toggle buyToggle;

        [SerializeField, Required, BoxGroup(GroupID = "Buy")]
        private GameObject buyPanel;

        private void Start()
        {
            closeButton.onClick.AddListener(() => mainPanel.SetActive(false));
            openButton.onClick.AddListener(() => mainPanel.SetActive(true));
            
            donationsToggle.onValueChanged.AddListener(value =>
            {
                donationsMenu.gameObject.SetActive(value);
                if (value == false)
                {
                    subscribeStarToggle.isOn = false;
                    cryptoToggle.isOn = false;
                    return;
                }
                
                buyToggle.isOn = false;
                contributeToggle.isOn = false;
                feedbackToggle.isOn = false;
            });
            
            subscribeStarToggle.onValueChanged.AddListener(subscribeStarPanel.gameObject.SetActive);
            copySubscribeStar.onClick.AddListener(() =>
            {
                TextEditor te = new() { text = SubscribeStarLink };
                te.SelectAll();
                te.Copy();
            });

            openSubscribeStar.onClick.AddListener(() => Application.OpenURL(SubscribeStarLink));
            
            cryptoToggle.onValueChanged.AddListener(cryptoPanel.gameObject.SetActive);
            copyBitcoin.onClick.AddListener(() =>
            {
                TextEditor te = new() { text = BitCoinAddress };
                te.SelectAll();
                te.Copy();
            });
            copyEthereum.onClick.AddListener(() =>
            {
                TextEditor te = new() { text = EthereumAddress };
                te.SelectAll();
                te.Copy();
            });
            copyMonero.onClick.AddListener(() =>
            {
                TextEditor te = new() { text = MoneroAddress };
                te.SelectAll();
                te.Copy();
            });

            emailButtonInsideCrypto.onClick.AddListener(() =>
            {
                TextEditor te = new() { text = Email };
                te.SelectAll();
                te.Copy();
            });
            
            discordButtonInsideCrypto.onClick.AddListener(() => Application.OpenURL(Discord));
            
            contributeToggle.onValueChanged.AddListener(value =>
            {
                contributePanel.gameObject.SetActive(value);
                if (value == false)
                    return;
                
                buyToggle.isOn = false;
                donationsToggle.isOn = false;
                feedbackToggle.isOn = false;
                subscribeStarToggle.isOn = false;
                cryptoToggle.isOn = false;
            });
            
            emailButtonInsideContribute.onClick.AddListener(() =>
            {
                TextEditor te = new() { text = Email };
                te.SelectAll();
                te.Copy();
            });
            
            discordButtonInsideContribute.onClick.AddListener(() => Application.OpenURL(Discord));
            
            feedbackToggle.onValueChanged.AddListener(value =>
            {
                feedbackPanel.gameObject.SetActive(value);
                if (value == false)
                    return;
                
                buyToggle.isOn = false;
                donationsToggle.isOn = false;
                contributeToggle.isOn = false;
                subscribeStarToggle.isOn = false;
                cryptoToggle.isOn = false;
            });
            
            emailButtonInsideFeedback.onClick.AddListener(() =>
            {
                TextEditor te = new() { text = Email };
                te.SelectAll();
                te.Copy();
            });
            
            discordButtonInsideFeedback.onClick.AddListener(() => Application.OpenURL(Discord));
            
            buyToggle.onValueChanged.AddListener(value =>
            {
                buyPanel.gameObject.SetActive(value);
                if (value == false)
                    return;
                
                donationsToggle.isOn = false;
                contributeToggle.isOn = false;
                feedbackToggle.isOn = false;
                subscribeStarToggle.isOn = false;
                cryptoToggle.isOn = false;
            });
        }
    }
}