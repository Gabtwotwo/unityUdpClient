using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using UnityEngine.UIElements;
using System.Drawing;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameState gameState;
    public List<Player> PlayerList;
    public PlayerData playerData;
    public Message latestMessage;
    public GameState latestGameState;
    public NewPlayer latestArrivingPlayer;
    public LeavingPlayer latestLeavingPlayer;
    public bool doneWithArrivingPlayer = false;

    void Start()
    {
        udp = new UdpClient();

        udp.Connect("18.191.58.55", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");

        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 0.0333333f, 0.0333333f);

    }

    void OnDestroy()
    {
        udp.Dispose();
    }

    [Serializable]
    public struct receivedColor
    {
        public float R;
        public float G;
        public float B;
    }

    [Serializable]
    public class Player
    {
        public string id;
        public receivedColor color;
        public Vector3 position;
        public bool init = true;
        public GameObject cube = null;
    }

    [Serializable]
    public class NewPlayer
    {
        public Player newPlayer;
    }

    [Serializable]
    public class LeavingPlayer
    {
        public Player lostPlayer;
    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    [Serializable]
    public class Message
    {
        public commands cmd;
    }

    public enum commands
    {
        ARRIVING_CLIENT,
        GAME_UPDATE,
        LEAVING_CLIENT,
        CLIENT_ID
    };

    public struct PlayerData
    {
        public Vector3 playerLocation;
        public string heartbeat;
    }

    public struct UniqueID
    {
        public string uniqueID;
    }

    UniqueID uniqueID;



    void OnReceived(IAsyncResult result)
    {
        UdpClient socket = result.AsyncState as UdpClient;

        IPEndPoint source = new IPEndPoint(0, 0);

        byte[] message = socket.EndReceive(result, ref source);

        string returnData = Encoding.ASCII.GetString(message);

        latestMessage = JsonUtility.FromJson<Message>(returnData);
        Debug.Log(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.ARRIVING_CLIENT:
                    latestArrivingPlayer = JsonUtility.FromJson<NewPlayer>(returnData);
                    doneWithArrivingPlayer = true;
                    break;

                case commands.GAME_UPDATE:
                    latestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;

                case commands.LEAVING_CLIENT:
                    latestLeavingPlayer = JsonUtility.FromJson<LeavingPlayer>(returnData);
                    break;

                case commands.CLIENT_ID:
                    uniqueID = JsonUtility.FromJson<UniqueID>(returnData);
                    break;
                default:
                    Debug.Log("Error: " + returnData);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {
        if (doneWithArrivingPlayer)
        {
            PlayerList.Add(latestArrivingPlayer.newPlayer);
            PlayerList.Last().cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PlayerList.Last().cube.AddComponent<PlayerCharacter>();

            doneWithArrivingPlayer = false;
        }
    }

    void UpdatePlayers()
    {
        for (int i = 0; i < latestGameState.players.Length; i++)
        {
            for (int j = 0; j < PlayerList.Count(); j++)
            {
                if (latestGameState.players[i].id == PlayerList[j].id)
                {
                    PlayerList[j].color = latestGameState.players[i].color;
                    PlayerList[j].cube.GetComponent<PlayerCharacter>().playerRef = PlayerList[j];
                    PlayerList[j].cube.transform.position = latestGameState.players[i].position;
                }
            }
        }
    }

    void DestroyPlayers()
    {
        foreach (Player player in PlayerList)
        {
            if (player.id == latestLeavingPlayer.lostPlayer.id)
            {
                Debug.Log(player.id);
                Debug.Log(PlayerList);
                PlayerList.Remove(player);
            }
        }
    }
    void HeartBeat()
    {
        playerData.playerLocation = new Vector3(0.0f, 0.0f, 0.0f);

        foreach (Player player in PlayerList)
        {
            if (player.id == uniqueID.uniqueID)
            {
                playerData.playerLocation = player.cube.transform.position;
                continue;
            }
        }

        playerData.heartbeat = "heartbeat";

        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(playerData));
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}