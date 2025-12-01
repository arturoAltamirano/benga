using UnityEngine;

public class Connector : MonoBehaviour
{
    public ConnectorPosition connectorPosition;

    [HideInInspector] public bool IsOccupied = false;

    public ConnectorType connectorType = ConnectorType.Vertical;

    public static ConnectorPosition GetOpposite(ConnectorPosition pos)
    {
        return pos switch
        {
            ConnectorPosition.left => ConnectorPosition.right,
            ConnectorPosition.right => ConnectorPosition.left,
            ConnectorPosition.top => ConnectorPosition.bottom,
            ConnectorPosition.bottom => ConnectorPosition.top,
            ConnectorPosition.front => ConnectorPosition.back,
            ConnectorPosition.back => ConnectorPosition.front,
            _ => pos,
        };
    }

    public static bool IsValidHorizontalAttachment(ConnectorPosition ghostPos, ConnectorPosition targetPos)
    {
        return targetPos switch
        {
            ConnectorPosition.left or ConnectorPosition.right => ghostPos == ConnectorPosition.top,// left/right connectors accept ghost TOP only
            ConnectorPosition.front or ConnectorPosition.back => ghostPos == ConnectorPosition.bottom,// front/back connectors accept ghost BOTTOM only
            _ => false,
        };
    }

}

public enum ConnectorType
{
    Vertical,
    Horizontal,
    DefenseObject
}

public enum ConnectorPosition
{
    left,
    right,
    top,
    bottom,
    front,
    back
}