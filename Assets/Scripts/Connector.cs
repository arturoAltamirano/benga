using UnityEngine;

public class Connector : MonoBehaviour
{
    public ConnectorPosition connectorPosition;

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
        switch (targetPos)
        {
            case ConnectorPosition.left:
            case ConnectorPosition.right:
                // left/right connectors accept ghost TOP only
                return ghostPos == ConnectorPosition.top;

            case ConnectorPosition.front:
            case ConnectorPosition.back:
                // front/back connectors accept ghost BOTTOM only
                return ghostPos == ConnectorPosition.bottom;

            default:
                return false;
        }
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