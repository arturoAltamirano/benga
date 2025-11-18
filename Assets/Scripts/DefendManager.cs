using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class DefendManager : MonoBehaviour
{
    [System.Serializable]

    public enum SelectedBuildType
    {
        VerticalPillar,

        HorizontalPillar,

        DefenseObject
    }

    [Header("Build Objects")]
    [SerializeField] private GameObject horizontalPillar;
    [SerializeField] private GameObject verticalPillar;
    [SerializeField] private GameObject defenseObject;

    [HideInInspector] private GameObject currentBuild;

    [Header("Build Settings")]
    [HideInInspector] private SelectedBuildType currentBuildType = SelectedBuildType.VerticalPillar;
    [SerializeField] private LayerMask connectorLayer;

    [Header("Ghost Settings")]
    [SerializeField] private Material ghostMaterialValid;
    [SerializeField] private Material ghostMaterialInvalid;

    [Header("Internal State")]
    [HideInInspector] private GameObject ghostBuildGameObject;
    [HideInInspector] private bool isGhostInValidPosition = false;
    [HideInInspector] public bool defenseObjectPlaced = false;
    [HideInInspector] private bool snappedToConnector = false;
    [HideInInspector] private Transform ModelParent = null;
    [SerializeField] private RoundManager RoundManager;

    [Header("Build Limits & UI")]
    [SerializeField] private TextMeshProUGUI selectedObjectText;
    [HideInInspector] public List<GameObject> placedObjects = new List<GameObject>();
    [SerializeField] private TextMeshProUGUI navigationAid;

    private bool isEnabled = true;

    public bool isEnabledAccess
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    private void Update()
    {
        //if we are blocked by round manager
        if (!isEnabled) return;

        //if user is pressing space to switch to offense
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            //if user pressing space, and defenseObject has not been placed
            if (!defenseObjectPlaced) navigationAid.text = $"Place defenseObject to switch round!";

            //otherwise - we are good to yield to attacker
            else
            {
                //Debug.Log("Switching to attacker from defender.");
                RoundManager.SwitchPhase(RoundManager.Phase.Attacker);
            }
        }

        //this is what will likely execute most frequently during runtime
        else
        {
            //evaluate if user is changing the object being placed
            HandleBuildSelection();
                        
            if (ghostBuildGameObject == null)
                isGhostInValidPosition = false;

            if (Input.GetMouseButtonDown(0) && isGhostInValidPosition)
                placeBuild();

            snappedToConnector = false;

            //prevent a second from being placed - ??
            if (currentBuildType == SelectedBuildType.DefenseObject && defenseObjectPlaced)
            {
                DestroyGhost();
                isGhostInValidPosition = false;
                return;
            }

            createGhostPrefab(currentBuild);

            moveGhostPrefabToRaycast();
        }   
    }

    private void HandleBuildSelection()
    /*
    Called from update every frame - if the user clicks any of the 3 designated keys change to the proper build object
    */
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentBuildType = SelectedBuildType.VerticalPillar;
            currentBuild = verticalPillar;

            //wipe the ghost object (the old object's ghost)
            DestroyGhost();
            selectedObjectText.text = $"Placing: {currentBuildType}";
        }

        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentBuildType = SelectedBuildType.HorizontalPillar;
            currentBuild = horizontalPillar;

            DestroyGhost();
            selectedObjectText.text = $"Placing: {currentBuildType}";        
        }

        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentBuildType = SelectedBuildType.DefenseObject;
            currentBuild = defenseObject;

            DestroyGhost();
            selectedObjectText.text = $"Placing: {currentBuildType}";
        }
    }

    private void DestroyGhost()
    {
        if (ghostBuildGameObject != null)
        {
            Destroy(ghostBuildGameObject);
            ghostBuildGameObject = null;
        }
    }

    private void createGhostPrefab(GameObject currentBuild)
    {
        if (ghostBuildGameObject == null)
        {
            ghostBuildGameObject = Instantiate(currentBuild);

            ModelParent = ghostBuildGameObject.transform.GetChild(0);

            //ghostifyModel(ModelParent, ghostMaterialValid);

            ghostifyModel(ghostBuildGameObject.transform);
        }
    }

    private void ghostifyModel(Transform modelParent, Material ghostMaterial = null)
    {
        if (ghostMaterial != null)
        {
            foreach (MeshRenderer meshRenderer in modelParent.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = ghostMaterial;
            }
        }

        else
        {
            foreach (Collider modelColliders in modelParent.GetComponentsInChildren<Collider>())
            {
                modelColliders.enabled = false;
            }
        }

        foreach (Rigidbody rb in modelParent.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }
    }

    private Bounds GetObjectBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);

        Bounds totalBounds = renderers[0].bounds;
        foreach (Renderer rend in renderers)
        {
            totalBounds.Encapsulate(rend.bounds);
        }
        return totalBounds;
    }

    private void moveGhostPrefabToRaycast()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        Debug.DrawRay(ray.origin, ray.direction * 200f, Color.red);

        // 1. Check if ray hits anything
        if (!Physics.Raycast(ray, out hit))
        {
            isGhostInValidPosition = false;
            ghostifyModel(ModelParent, ghostMaterialInvalid);
            return;
        }

        // 2. Align ghost to surface normal
        ghostBuildGameObject.transform.up = hit.normal;

        Bounds bounds = GetObjectBounds(ghostBuildGameObject);
        float offset;

        if (currentBuildType == SelectedBuildType.HorizontalPillar)
        {
            offset = Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        else
        {
            offset = bounds.extents.y;
        }

        ghostBuildGameObject.transform.position = hit.point + hit.normal * offset;

        // 4. Try snapping to connectors
        TrySnapGhostToConnector(hit);

        // 5. If still not valid, allow ground placement
        if (!isGhostInValidPosition)
        {

            //Debug.LogWarning($"Placed on Ground");

            isGhostInValidPosition = true; // optional: allow placing on flat surfaces
            ghostifyModel(ModelParent, ghostMaterialValid);
        }
    }

    private void SnapGhostToConnector(Connector ghost, Connector target)
    {
        Debug.LogWarning($"TrySnap - Ghost Connector: {ghost} ---> Target Connector {target}");
        Transform ghostRoot = ghostBuildGameObject.transform;

        // 1. ROTATION: align connectors
        Quaternion rotationDelta = target.transform.rotation * Quaternion.Inverse(ghost.transform.rotation);
        ghostRoot.rotation = rotationDelta * ghostRoot.rotation;

        // 2. POSITION: overlap connectors
        Vector3 positionDelta = target.transform.position - ghost.transform.position;
        ghostRoot.position += positionDelta;

        snappedToConnector = true;
        isGhostInValidPosition = true;
        ghostifyModel(ModelParent, ghostMaterialValid);

        return;
    }

    private void TrySnapGhostToConnector(RaycastHit hit)
    {
        float snapRadius = 5f;

        if (currentBuildType == SelectedBuildType.VerticalPillar || currentBuildType == SelectedBuildType.DefenseObject)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, snapRadius, connectorLayer);
            Connector targetConnector = null;

            foreach (Collider col in nearbyColliders)
            {
                Connector c = col.GetComponent<Connector>();
                if (c != null)
                {
                    targetConnector = c;
                    break;
                }
            }

            if (targetConnector == null)
            {
                isGhostInValidPosition = false;
                snappedToConnector = false;
                return;
            }

            Connector[] ghostConnectors;
            Connector ghostConnector = null; 
            bool targetIsHorizontal = Mathf.Abs(targetConnector.transform.up.y) < 0.5f;

            if (targetIsHorizontal && currentBuildType == SelectedBuildType.VerticalPillar)
            {
                // Vertical pillar attaching to horizontal → always use BACK connector
                ConnectorPosition desiredGhostConnectorPos = ConnectorPosition.front;

                ghostConnectors = ghostBuildGameObject.GetComponentsInChildren<Connector>();

                foreach (var gc in ghostConnectors)
                {
                    if (gc.connectorPosition == desiredGhostConnectorPos)
                    {
                        ghostConnector = gc;
                        break;
                    }
                }

                if (ghostConnector == null)
                {
                    isGhostInValidPosition = false;
                    snappedToConnector = false;
                    return;
                }
            }

            else
            {
                // Normal opposite-connector logic
                ConnectorPosition opposite = Connector.GetOpposite(targetConnector.connectorPosition);

                ghostConnectors = ghostBuildGameObject.GetComponentsInChildren<Connector>();

                foreach (var gc in ghostConnectors)
                {
                    if (gc.connectorPosition == opposite)
                    {
                        ghostConnector = gc;
                        break;
                    }
                }

                if (ghostConnector == null)
                {
                    isGhostInValidPosition = false;
                    snappedToConnector = false;
                    return;
                }
            }

            // Now ghostConnector is ALWAYS valid if we got here
            SnapGhostToConnector(ghostConnector, targetConnector);

            ghostifyModel(ModelParent, ghostMaterialValid);
            isGhostInValidPosition = true;
        }

        ////////////////////////////////////////////////////////////////////////////

        else if (currentBuildType == SelectedBuildType.HorizontalPillar)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, snapRadius, connectorLayer);
            Connector targetConnector = null;

            // Find ANY side connector in range
            foreach (Collider col in nearbyColliders)
            {
                Connector c = col.GetComponent<Connector>();
                if (c != null && !c.isOccupied)
                {
                    // Must be a side connector
                    if (c.connectorPosition == ConnectorPosition.left ||
                        c.connectorPosition == ConnectorPosition.right ||
                        c.connectorPosition == ConnectorPosition.front ||
                        c.connectorPosition == ConnectorPosition.back)
                    {
                        targetConnector = c;
                        break;
                    }
                }
            }

            if (targetConnector == null)
            {
                isGhostInValidPosition = false;
                snappedToConnector = false;
                return;
            }

            // Search the ghost object for TOP or BOTTOM connector
            Connector[] ghostConnectors = ghostBuildGameObject.GetComponentsInChildren<Connector>();
            Connector ghostConnector = null;

            foreach (var gc in ghostConnectors)
            {
                if (gc.connectorType != ConnectorType.Horizontal)
                    continue;

                if (Connector.IsValidHorizontalAttachment(gc.connectorPosition, targetConnector.connectorPosition))
                {
                    ghostConnector = gc;
                    break;
                }
            }

            if (ghostConnector == null)
            {
                isGhostInValidPosition = false;
                snappedToConnector = false;
                return;
            }

            // Snap into place
            SnapGhostToConnector(ghostConnector, targetConnector);

            ghostifyModel(ModelParent, ghostMaterialValid);
            isGhostInValidPosition = true;
        }

        return;
    }

    private void MarkConnectorsAsOccupied(GameObject placedObject)
    {
        Connector[] conns = placedObject.GetComponentsInChildren<Connector>();
        foreach (var c in conns)
            c.isOccupied = true;
    }

    private void AttachJointToNearbyConnector(GameObject placedObj)
    {
        Connector[] objConnectors = placedObj.GetComponentsInChildren<Connector>();
        foreach (var c in objConnectors)
        {
            // Find the closest world connector (already compatible)
            Collider[] nearby = Physics.OverlapSphere(c.transform.position, 0.1f, connectorLayer);
            foreach (var col in nearby)
            {
                Connector target = col.GetComponent<Connector>();
                if (target != null)
                {
                    Rigidbody targetRb = target.GetComponentInParent<Rigidbody>();
                    Rigidbody placedRb = placedObj.GetComponent<Rigidbody>();

                    if (targetRb != null && placedRb != null && targetRb != placedRb)
                    {
                        Debug.Log($"Attached to Connector. Target Rb: {targetRb} | Placed Rb {placedRb}");
                        FixedJoint joint = placedObj.AddComponent<FixedJoint>();
                        joint.connectedBody = targetRb;
                        //Destroy(target);
                        joint.breakForce = 10000f;
                        joint.breakTorque = 10000f;
                    }
                }
            }
        }
    }

    private void placeBuild()
    {
        snappedToConnector = false;

        if (currentBuildType == SelectedBuildType.DefenseObject && defenseObjectPlaced)
        {
            Debug.Log("Defense object already placed! Cannot place another.");
            Destroy(ghostBuildGameObject);
            ghostBuildGameObject = null;
            return;
        }

        if (ghostBuildGameObject != null && isGhostInValidPosition)
        {
            //GameObject prefab = getCurrentBuild();
            Vector3 placePos = ghostBuildGameObject.transform.position;
            Quaternion placeRot = ghostBuildGameObject.transform.rotation;

            GameObject newBuild = Instantiate(currentBuild, placePos, placeRot);

            if (currentBuildType == SelectedBuildType.DefenseObject)
                defenseObjectPlaced = true;


            Rigidbody rb = newBuild.GetComponent<Rigidbody>();

            placedObjects.Add(newBuild);

            // Mark connectors
            MarkConnectorsAsOccupied(newBuild);

            if (rb != null)
            {

                rb.isKinematic = false;
                rb.useGravity = true; 
                rb.detectCollisions = true;

                // Attach joint to any connected world connector
                AttachJointToNearbyConnector(newBuild);
            }


            if (!snappedToConnector)
            {
                Debug.Log("Ground anchor used");
                // Create a fake “ground anchor”
                GameObject groundAnchor = new GameObject("GroundAnchor");
                groundAnchor.transform.position = newBuild.transform.position;

                Rigidbody anchorRb = groundAnchor.AddComponent<Rigidbody>();
                anchorRb.isKinematic = true;

                FixedJoint joint = newBuild.AddComponent<FixedJoint>();
                joint.connectedBody = anchorRb;
                joint.breakForce = 2000f;
                joint.breakTorque = 2000f;
            }

            Destroy(ghostBuildGameObject);
            ghostBuildGameObject = null;
        }
    }
}