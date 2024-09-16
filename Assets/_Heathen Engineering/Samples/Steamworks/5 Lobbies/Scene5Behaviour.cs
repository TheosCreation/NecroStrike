#if HE_SYSCORE && (STEAMWORKSNET || FACEPUNCH) && !DISABLESTEAMWORKS 
using HeathenEngineering.SteamworksIntegration;
using UnityEngine;

namespace HeathenEngineering.DEMO
{
    [System.Obsolete("This script is for demonstration purposes ONLY")]
    public class Scene5Behaviour : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.UI.InputField lobbyIdField;
        [SerializeField]
        private UnityEngine.UI.Text chatText;
        [SerializeField]
        private LobbyManager lobbyManager;

        private void Start()
        {
            lobbyManager.evtCreated.AddListener(HandleLobbyCreated);
            lobbyManager.evtEnter.AddListener(HandleLobbyEnter);
            lobbyManager.evtUserJoined.AddListener(HandleSomeOneJoined);
        }

        private void HandleSomeOneJoined(UserData arg0)
        {
            Debug.Log(arg0.Name + " joined the lobby, you can iterate over the list of members easily ... just check the comments for details");
            Debug.Log("The members are:");
            var lobbyReference = lobbyManager.Lobby;
            foreach(var member in lobbyReference.Members)
            {
                Debug.Log(member.user.Name);
            }
        }

        private void HandleLobbyEnter(Steamworks.LobbyEnter_t arg0)
        {
            Debug.Log($"You just entered a lobby, the system is now managing {HeathenEngineering.SteamworksIntegration.API.Matchmaking.Client.memberOfLobbies.Count} lobbies.");
        }

        private void HandleLobbyCreated(Lobby arg0)
        {
            var id = arg0.id;
            Debug.Log("On Handle Lobby Created: a new lobby has been created with CSteamID = " + arg0.ToString()
                + "\nThe CSteamID can be broken down into its parts such as :"
                + "\nAccount Type = " + id.GetEAccountType()
                + "\nAccount Instance = " + id.GetUnAccountInstance()
                + "\nUniverse = " + id.GetEUniverse()
                + "\nAccount Id = " + id.GetAccountID()
                + "\nThis can be used to reconstruct the ID ... for example you can assume all values excep the Account Id, thus a user can type in `" + id.GetAccountID() + "` and you can reconstruct the full ID of " + arg0.ToString() + " by using the command Lobby.Get(" + id.GetAccountID() + ");");
        }

        public void OpenKnowledgeBaseLobbies()
        {
            Application.OpenURL("https://kb.heathenengineering.com/assets/steamworks/features/multiplayer");
        }

        public void OpenKnowledgeBaseLobbyManager()
        {
            Application.OpenURL("https://kb.heathenengineering.com/assets/steamworks/components/lobby-manager");
        }
        
        public void OpenKnowledgeInspector()
        {
            Application.OpenURL("https://kb.heathenengineering.com/assets/steamworks/quick-start-guide#debugging");
        }

        public void ReportSearchResults(Lobby[] results)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Found: " + results.Length);
            for (int i = 0; i < results.Length; i++)
            {
                var name = results[i].Name;
                if (string.IsNullOrEmpty(name))
                    name = "UNKNOWN";
                sb.Append("\n " + results[i].id + ", name = " + name);
            }

            Debug.Log(sb.ToString());
        }

        public void JoinLobby()
        {
            lobbyManager.Join(lobbyIdField.text);
        }

        public void HandleChatMessages(LobbyChatMsg message)
        {
            chatText.text += "\n" + message.sender.Name + " said: " + message.Message;
        }

        [ContextMenu("Set Ready")]
        public void SetReady()
        {
            var lobby = lobbyManager.Lobby;
            lobby.IsReady = !lobbyManager.Lobby.IsReady;
        }
    }
}
#endif
