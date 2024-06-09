using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class RoomNodeGraphEditor : EditorWindow
{


    private GUIStyle roomNodeStyle;
    private GUIStyle roomNodeSelectedStyle;
    private static RoomNodeGraphSO currentRoomNodeGraph;
    private RoomNodeTypeListSO roomNodeTypeList;
    private RoomNodeSO currentRoomNode = null;
    private const float connectingLineWidth = 3f;
    private const float connectingArrowSize = 6f;

    private Vector2 graphOffset;
    private Vector2 graphDrag;



    // Node layout values
    private const float nodeWidth = 160f;
    private const float nodeHeight = 75f;
    private const int nodePadding = 25;
    private const int nodeBorder = 12;

    private const float gridLarge = 100f;
    private const float gridSmall = 25f;

    [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
    private static void OpenWindow()
    {
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }


    private void OnEnable()
    {
        Selection.selectionChanged += InspectorSelectionChanged;

        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        roomNodeStyle.normal.textColor = Color.white;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        roomNodeSelectedStyle = new GUIStyle();
        roomNodeSelectedStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
        roomNodeSelectedStyle.normal.textColor = Color.white;
        roomNodeSelectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodeBorder);
        roomNodeSelectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);



        // Load Room Node Types
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }


    private void OnDisable() {
        Selection.selectionChanged -= InspectorSelectionChanged;
    }


    /**
     * Open a Room Node Graph Editor if a Room Node Graph Scriptable Object is double clicked
     */
    [OnOpenAsset(0)]
    public static bool OnDoubleClickAssets(int instanceID, int line)
    {

        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            OpenWindow();
            currentRoomNodeGraph = roomNodeGraph;
            return true;
        }

        return false;
    }


    private void OnGUI()
    {

        if (currentRoomNodeGraph != null)
        {

            DrawBackgroundGrid(gridSmall, 0.2f, Color.gray);
            DrawBackgroundGrid(gridLarge, 0.3f, Color.gray);

            DrawDraggedLine();

            // Process events
            ProcessEvents(Event.current);

            DrawRoomNodeConnections();

            DrawRoomNodes();
        }

        if (GUI.changed)
        {
            Repaint();
        }

    }

    private void DrawBackgroundGrid(float gridSize, float gridOpacity, Color gridColor) {

        int verticalLineCount = Mathf.CeilToInt((position.width + gridSize) / gridSize);
        int horizontalLineCount = Mathf.CeilToInt((position.height + gridSize) / gridSize);

        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        graphOffset += graphDrag * 0.5f;

        Vector3 gridOffset = new Vector3(graphOffset.x % gridSize, graphOffset.y % gridSize, 0);

        for (int i = 0; i < verticalLineCount; i++) {
            Handles.DrawLine(new Vector3(gridSize * i, -gridSize, 0f) + gridOffset, new Vector3(gridSize * i, position.height + gridSize, 0f) + gridOffset);
        }

        for (int j = 0; j < horizontalLineCount; j++) {
            Handles.DrawLine(new Vector3(-gridSize, gridSize * j, 0f) + gridOffset, new Vector3(position.width + gridSize, gridSize * j, 0f) + gridOffset);
        }

        Handles.color = Color.white;

    }


    private void DrawDraggedLine() {
        if (currentRoomNodeGraph.linePosition != Vector2.zero) {
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeTodrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, 
                currentRoomNodeGraph.roomNodeTodrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, 
                Color.white, null, connectingLineWidth);
        }
    }

    private void ProcessEvents(Event currentEvent)
    {

        graphDrag = Vector2.zero;

        if (currentRoomNode == null || currentRoomNode.isLeftClickDragging == false) {
            currentRoomNode = IsMouseOverRoomNode(currentEvent);
        }

        if (currentRoomNode == null || currentRoomNodeGraph.roomNodeTodrawLineFrom != null) 
        {
            ProcessRoomNodeGraphEvents(currentEvent);
        } else {
            currentRoomNode.ProcessEvents(currentEvent);
        }
    }


    private RoomNodeSO IsMouseOverRoomNode(Event currentEvent) {

        for (int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--) 
        {
            if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition)) {
                return currentRoomNodeGraph.roomNodeList[i];
            }
        }

        return null;
    }



    private void ProcessRoomNodeGraphEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;
            case EventType.MouseUp :
                ProcessMouseUpEvent(currentEvent);
                break;
            case EventType.MouseDrag :
                ProcessMouseDragEvent(currentEvent);
                break;
            default:
                break;
        }
    }



    private void ProcessMouseDownEvent(Event currentEvent)
    {

        if (currentEvent.button == 1)
        {
            ShowContextMenu(currentEvent.mousePosition);
        } else if (currentEvent.button == 0) {
            ClearLineDrag();
            ClearAllSelectedRoomNodes();
        }
    }


    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Delete Selected Room Node Links"), false, DeleteSelectedRoomNodeLinks);
        menu.AddItem(new GUIContent("Delete Selected Room Nodes"), false, DeleteSelectedRoomNodes);


        menu.ShowAsContext();
    }

    private void SelectAllRoomNodes() {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList) {
            roomNode.isSelected = true;
        }
        GUI.changed = true;
    }

    private void DeleteSelectedRoomNodeLinks() {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList) {

            if (roomNode.isSelected && roomNode.childRoomNodeIDList.Count > 0) {

                for (int i = roomNode.childRoomNodeIDList.Count -1; i >= 0; i--) {
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(roomNode.childRoomNodeIDList[i]);

                    if (childRoomNode != null && childRoomNode.isSelected) {
                        roomNode.RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);

                        childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }

                }
            }

        }

        ClearAllSelectedRoomNodes();
    }


    private void DeleteSelectedRoomNodes() {
        Queue<RoomNodeSO> roomNodeDeletionQueue = new Queue<RoomNodeSO>();
        
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList) {
            if (roomNode.isSelected && !roomNode.roomNodeType.isEntrance) {
                roomNodeDeletionQueue.Enqueue(roomNode);

                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList) {
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(childRoomNodeID);
                    if (childRoomNode != null) {
                        childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }

                }

                foreach (string parentRoomNodeID in roomNode.parentRoomNodeIDList) {

                    RoomNodeSO parentRoomNode = currentRoomNodeGraph.GetRoomNode(roomNode.id);

                    if (parentRoomNode !=  null) {
                        parentRoomNode.RemoveChildRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
            }
        }

        while (roomNodeDeletionQueue.Count > 0) {
            RoomNodeSO roomNodeToDelete = roomNodeDeletionQueue.Dequeue();

            currentRoomNodeGraph.roomNodeDictionary.Remove(roomNodeToDelete.id);

            currentRoomNodeGraph.roomNodeList.Remove(roomNodeToDelete);

            DestroyImmediate(roomNodeToDelete, true);

            AssetDatabase.SaveAssets();
        }
    }


    private void CreateRoomNode(object mousePositionObject)
    {
        if (currentRoomNodeGraph.roomNodeList.Count == 0) {
            CreateRoomNode(new Vector2(200f, 200f), roomNodeTypeList.list.Find(x => x.isEntrance));
        }

        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }



    private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2) mousePositionObject;

        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        roomNode.Initialise(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);

        AssetDatabase.SaveAssets();

        // Refresh dictionnary
        currentRoomNodeGraph.OnValidate();
    }


    private void ClearAllSelectedRoomNodes() {

        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList) {
            if (roomNode.isSelected) {
                roomNode.isSelected = false;
                GUI.changed = true;
            }
        }
    }


    private void ProcessMouseUpEvent(Event currentEvent) {

        if (currentEvent.button == 1 && currentRoomNodeGraph.roomNodeTodrawLineFrom != null) {

            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);

            if (roomNode != null) {
                if (currentRoomNodeGraph.roomNodeTodrawLineFrom.AddChildRoomNodeIDToRoomNode(roomNode.id)) {
                    roomNode.AddParentRoomNodeIDToRoomNode(currentRoomNodeGraph.roomNodeTodrawLineFrom.id);
                }
            }

            ClearLineDrag();
        }
    }


    private void ClearLineDrag() {
        currentRoomNodeGraph.roomNodeTodrawLineFrom = null;
        currentRoomNodeGraph.linePosition = Vector2.zero;
        GUI.changed = true;
    }


    private void DrawRoomNodeConnections() {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList) {
            if (roomNode.childRoomNodeIDList.Count > 0) {

                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList) {
                    if (currentRoomNodeGraph.roomNodeDictionary.ContainsKey(childRoomNodeID)) {
                        DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childRoomNodeID]);

                        GUI.changed = true;
                    }
                }
            }
        }
    }


    private void DrawConnectionLine(RoomNodeSO parentRoomNode, RoomNodeSO childRoomNode) {

        Vector2 startPosition = parentRoomNode.rect.center;
        Vector2 endPosition = childRoomNode.rect.center;

        Vector2 midPosition = (endPosition + startPosition) / 2;

        Vector2 direction = endPosition - startPosition;

        Vector2 arrowTailPoint1 = midPosition - new Vector2(-direction.y, direction.x).normalized * connectingArrowSize;
        Vector2 arrowTailPoint2 = midPosition + new Vector2(-direction.y, direction.x).normalized * connectingArrowSize;

        Vector2 arrowHeadPoint = midPosition + direction.normalized * connectingArrowSize;

        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, Color.white, null, connectingLineWidth);
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, Color.white, null, connectingLineWidth);

        Handles.DrawBezier(startPosition, endPosition, startPosition, endPosition, Color.white, null, connectingLineWidth);

        GUI.changed = true;
    }


    private void ProcessMouseDragEvent(Event currentEvent) {

        if (currentEvent.button == 1) {
            ProcessRightMouseDragEvent(currentEvent);
        }

        if (currentEvent.button == 0) {
            ProcessLeftMouseDragEvent(currentEvent.delta);
        }
    }

    
    private void ProcessRightMouseDragEvent(Event currentEvent) {
        if (currentRoomNodeGraph.roomNodeTodrawLineFrom != null) {
            DragConnectionLine(currentEvent.delta);
            GUI.changed = true;
        }
    }


    private void ProcessLeftMouseDragEvent(Vector2 dragDelta) {

        graphDrag = dragDelta;

        for (int i = 0; i < currentRoomNodeGraph.roomNodeList.Count; i++) {
            currentRoomNodeGraph.roomNodeList[i].DragNode(dragDelta);
        }

        GUI.changed = true;
    }

    private void DragConnectionLine(Vector2 delta) {
        currentRoomNodeGraph.linePosition += delta;
    }


    private void DrawRoomNodes()
    {

        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {

            if (roomNode.isSelected) {
                roomNode.Draw(roomNodeSelectedStyle);
            } else {
                roomNode.Draw(roomNodeStyle);
            }

            roomNode.Draw(roomNodeStyle);
        }

        GUI.changed = true;

    }

    private void InspectorSelectionChanged() {

        RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

        if (roomNodeGraph != null) {
            currentRoomNodeGraph = roomNodeGraph;
            GUI.changed = true;
        }
    }
}
