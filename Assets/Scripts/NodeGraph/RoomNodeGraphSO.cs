using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomNodeGraph", menuName = "Scriptable Objects/Dungeon/Room Node Graph")]
public class RoomNodeGraphSO : ScriptableObject
{
    [HideInInspector]
    public RoomNodeTypeListSO roomNodeTypeList;
    [HideInInspector]
    public List<RoomNodeSO> roomNodeList = new List<RoomNodeSO>();
    [HideInInspector]
    public Dictionary<string, RoomNodeSO> roomNodeDictionary = new Dictionary<string, RoomNodeSO>();


    private void Awake() {
        LoadRoomNodeDictionary();
    }


    private void LoadRoomNodeDictionary() {

        roomNodeDictionary.Clear();

        foreach (RoomNodeSO node in roomNodeList) {
            roomNodeDictionary[node.id] = node;
        }

    }


    public RoomNodeSO GetRoomNode(string roomNodeID) {
        if (roomNodeDictionary.TryGetValue(roomNodeID, out RoomNodeSO roomNode)) {
            return roomNode;
        }
        return null;
    }


#region Editor Code
#if UNITY_EDITOR
    [HideInInspector]
    public RoomNodeSO roomNodeTodrawLineFrom = null;
    [HideInInspector]
    public Vector2 linePosition;


    public void OnValidate() {
        LoadRoomNodeDictionary();
    }


    public void SetNodeToDrawConnectionLineFrom(RoomNodeSO node, Vector2 position) {

        roomNodeTodrawLineFrom = node;
        linePosition = position;
    }


#endif
#endregion

}
