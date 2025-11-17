using UnityEngine;

public class Connector : MonoBehaviour
{
    public ConnectorPosition connectorPosition;

    public ConnectorType connectorType = ConnectorType.Vertical;

    [HideInInspector] public bool isOccupied = false;

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
        bool ghostIsTopBottom =
            ghostPos == ConnectorPosition.top ||
            ghostPos == ConnectorPosition.bottom;

        bool targetIsSide =
            targetPos == ConnectorPosition.left ||
            targetPos == ConnectorPosition.right ||
            targetPos == ConnectorPosition.front ||
            targetPos == ConnectorPosition.back;

        return ghostIsTopBottom && targetIsSide;
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