using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

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
    [HideInInspector] public Connector targetConnector = null;
    [HideInInspector] public Connector ghostConnector = null;
    [SerializeField] public float breakForce;
    [SerializeField] public float breakTorque;

    [Header("Ghost Settings")]
    [SerializeField] private Material ghostMaterialValid;
    [SerializeField] private Material ghostMaterialInvalid;

    [Header("Internal State")]
    [HideInInspector] private GameObject ghostBuildGameObject;
    [HideInInspector] private bool isGhostInValidPosition = false;
    [HideInInspector] public bool defenseObjectPlaced = false;
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
                //destroy any ghost currently being displayed
                DestroyGhost();

                //Debug.Log("Switching to attacker from defender.");
                RoundManager.SwitchPhase(RoundManager.Phase.Attacker);
            }
        }

        //this is what will likely execute most frequently during runtime
        else
        {
            selectedObjectText.text = $"Placing: {currentBuildType}";

            //evaluate if user is changing the object being placed
            HandleBuildSelection();

            //both set in HandleBuild - if valid we're safe to proceed
            if (ghostBuildGameObject != null && ModelParent != null)
            {
                //continously update the position of the ghost to where user is raycasting
                moveGhostPrefabToRaycast();

                //if user is clicking to place the object, and it is in valid position
                if (Input.GetMouseButtonDown(0) && isGhostInValidPosition)
                {
                    PlaceBuild();

                    //we still need to create a new ghost, after destroying the previous following placement
                    CreateGhost(currentBuild);
                }
            }
        }
    }

    private void DestroyGhost()
    //small helper method to destory the ghostModel of our building block, and set it to null for reinstantiation
    {
        if (ghostBuildGameObject != null)
        {
            Destroy(ghostBuildGameObject);

            //set this to null for safety check in create
            ghostBuildGameObject = null;
        }
    }

    private void CreateGhost(GameObject currentBuild)
    //small helper method to instantiate the ghostModel of our building block
    {
        if (ghostBuildGameObject ==  null)
        {
            //if ghost is null - we need to instantiate it to the current selected model
            ghostBuildGameObject = Instantiate(currentBuild);
            ModelParent = ghostBuildGameObject.transform.GetChild(0);
            ghostifyModel(ghostBuildGameObject.transform);  
        }
    }

    private void ghostifyModel(Transform modelParent, Material ghostMaterial = null)
    /*
    note for later: it would be much better to make a prefab and instantiate that - as opposed to doing these ops on current pillar prefabs...
    */
    {
        foreach (Rigidbody rb in modelParent.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

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

            //create new ghost for newly selected ghost
            CreateGhost(currentBuild);

            //selectedObjectText.text = $"Placing: {currentBuildType}";
        }

        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentBuildType = SelectedBuildType.HorizontalPillar;
            currentBuild = horizontalPillar;

            //wipe the ghost object (the old object's ghost)
            DestroyGhost();

            //create new ghost for newly selected ghost
            CreateGhost(currentBuild);

            //selectedObjectText.text = $"Placing: {currentBuildType}";        
        }

        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentBuildType = SelectedBuildType.DefenseObject;
            currentBuild = defenseObject;

            //wipe the ghost object (the old object's ghost)
            DestroyGhost();

            //create new ghost for newly selected ghost
            CreateGhost(currentBuild);

            //selectedObjectText.text = $"Placing: {currentBuildType}";
        }
    }

    private Bounds GetObjectBounds(GameObject obj)
    /*
    Grow the bounds of our object with respect to every renderer within it, captures all connectors 
    and other aspects of the prefab. Maybe take a look at this later, it may be unneccesary.
    */
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
        if (ghostBuildGameObject == null || ModelParent == null)
            return;

        //project our ray from viewport
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        //if our ray does not hit a valid surface, set the material to invalid (red)
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            isGhostInValidPosition = false;
            ghostifyModel(ModelParent, ghostMaterialInvalid);
            return;
        }
        
        //to prevent the object from morphing into other objects at it's center, we need to 
        //apply an offset according to the bounds to visually force the element out of other elements
        Bounds bounds = GetObjectBounds(ghostBuildGameObject);
        float offset;

        //if this is a vertical pillar, we need to offset from the Y axis, this prevents the pillar morphing
        //into the floor when being transformed to the point of the ray cast
        if (currentBuildType == SelectedBuildType.VerticalPillar || currentBuildType == SelectedBuildType.DefenseObject)
        {
            offset = bounds.extents.y;
            ghostBuildGameObject.transform.position = hit.point + hit.normal * offset;
        }

        //else if this is a horizontal pillar, we now have the same problem but along a different set of axis
        //account for either the x or z, depending on orientation
        else if (currentBuildType == SelectedBuildType.HorizontalPillar)
        {
            offset = Mathf.Max(bounds.extents.x, bounds.extents.z);
            ghostBuildGameObject.transform.position = hit.point + hit.normal * offset;
        }

        //here is where it becomes complicated - we will try to move this object to a connector we may be looking at 
        TrySnapToConnector(currentBuildType, hit);
        //if the ghost is in a valid position now, set the color properly
        ghostifyModel(ModelParent, isGhostInValidPosition ? ghostMaterialValid : ghostMaterialInvalid);
    }

    private void SnapGhostToConnector(Connector ghost, Connector target, bool horizontalToVertical, bool verticalToHorizontal)
    /*
    Take the ghost connector and target connector and move them onto one another, 
    thus snapping the whole prefab into position 
    */
    {
        Debug.Log($"SnapGhostToConnector - Ghost: {ghost} and Target: {target}");

        if(ghost == null || target == null) return;

        //start by getting the ghosts current position at this time
        Transform ghostRoot = ghostBuildGameObject.transform;
        Quaternion rotationDelta = target.transform.rotation * Quaternion.Inverse(ghost.transform.rotation);

        if (horizontalToVertical == false && verticalToHorizontal == false)
        {
            //we first want to align these connectors which will have inherently opposite rotations
            //since all connectors were placed as clones of one another, they all have the same rotational
            //orientation, but are place in different directions on their parent prefab
            ghostRoot = ghostBuildGameObject.transform;
            rotationDelta = target.transform.rotation * Quaternion.Inverse(ghost.transform.rotation);
            ghostRoot.rotation = rotationDelta * ghostRoot.rotation;

            //move the ghost connector to the target connector, now with the proper rotation
            //this moves the whole block into place, and provides a good point to anchor at
            Vector3 positionDelta = target.transform.position - ghost.transform.position;
            ghostRoot.position += positionDelta;

            //set this true since we must be in a valid point if we've snapped 
            //and make sure we're still displaying as a ghost
            isGhostInValidPosition = true;
            ghostifyModel(ModelParent, ghostMaterialValid);
        }
        

        //we need to handle horizontal beams - works perfectly for vertical but puts 
        //horizontals into the floor as is - this is a rotation issue
        else if (horizontalToVertical)
        {
            if (target.connectorPosition == ConnectorPosition.right) 
                ghostRoot.Rotate(90f, 0f, 0f, Space.Self);
                //extra = Quaternion.Euler(90f, 0f, 0f);

            else if (target.connectorPosition == ConnectorPosition.left)
                ghostRoot.Rotate(-90f, 0f, 0f, Space.Self);
                //extra = Quaternion.Euler(-90f, 0f, 0f);
            
            else if (target.connectorPosition == ConnectorPosition.front)
                ghostRoot.Rotate(0f, 0f, -90f, Space.Self);
                //extra = Quaternion.Euler(0f, 0f, 90f);

            else if (target.connectorPosition == ConnectorPosition.back)
                ghostRoot.Rotate(0f, 0f, 90f, Space.Self);  
                //extra = Quaternion.Euler(0f, 0f, -90f);

            ghostRoot.rotation = rotationDelta * ghostRoot.rotation;
            //ghostRoot.rotation = rotationDelta * extra;

            //move the ghost connector to the target connector, now with the proper rotation
            //this moves the whole block into place, and provides a good point to anchor at
            Vector3 positionDelta = target.transform.position - ghost.transform.position;
            ghostRoot.position += positionDelta;

            //set this true since we must be in a valid point if we've snapped 
            //and make sure we're still displaying as a ghost
            isGhostInValidPosition = true;
            ghostifyModel(ModelParent, ghostMaterialValid);

            return;
        }

        else if (verticalToHorizontal)
        {
            if (target.connectorPosition == ConnectorPosition.top)
            {
                if (ghost.connectorPosition == ConnectorPosition.right) 
                    ghostRoot.Rotate(-90f, 0f, -90f, Space.Self);
                    //extra = Quaternion.Euler(90f, 0f, -90f);

                else if (ghost.connectorPosition == ConnectorPosition.left)
                    ghostRoot.Rotate(-90f, 0f, -90f, Space.Self);
                    //extra = Quaternion.Euler(-90f, 0f, -90f);
            
                else if (ghost.connectorPosition == ConnectorPosition.front)
                    ghostRoot.Rotate(0f, 0f, 90f, Space.Self);
                    //extra = Quaternion.Euler(0f, 0f, 90f);

                else if (ghost.connectorPosition == ConnectorPosition.back)
                    ghostRoot.Rotate(0f, 0f, -90f, Space.Self);  
                    //extra = Quaternion.Euler(0f, 0f, -90f);
            }

            else if (target.connectorPosition == ConnectorPosition.bottom)
            {
                if (ghost.connectorPosition == ConnectorPosition.right) 
                    ghostRoot.Rotate(-90f, 0f, 0f, Space.Self);
                    //extra = Quaternion.Euler(90f, 0f, -90f);

                else if (ghost.connectorPosition == ConnectorPosition.left)
                    ghostRoot.Rotate(-90f, 0f, -90f, Space.Self);
                    //extra = Quaternion.Euler(-90f, 0f, -90f);
            
                else if (ghost.connectorPosition == ConnectorPosition.front)
                    ghostRoot.Rotate(0f, 0f, 90f, Space.Self);
                    //extra = Quaternion.Euler(0f, 0f, 90f);

                else if (ghost.connectorPosition == ConnectorPosition.back)
                    ghostRoot.Rotate(0f, 0f, -90f, Space.Self);  
                    //extra = Quaternion.Euler(0f, 0f, -90f);
            }
            
            ghostRoot.rotation = rotationDelta * ghostRoot.rotation;

            //ghostRoot.rotation = rotationDelta * extra;

            //move the ghost connector to the target connector, now with the proper rotation
            //this moves the whole block into place, and provides a good point to anchor at
            Vector3 positionDelta = target.transform.position - ghost.transform.position;
            ghostRoot.position += positionDelta;

            //set this true since we must be in a valid point if we've snapped 
            //and make sure we're still displaying as a ghost
            isGhostInValidPosition = true;
            ghostifyModel(ModelParent, ghostMaterialValid);

            return;
        }
    }

    private void SetConnectorVertical(RaycastHit hit)
    {
        //we are going to define an overlap sphere positioned at our 'hit' point, this will collect 
        //all of the connectors within a given radius of our raycast 
        //this will tell us what the user might be looking at
        #pragma warning disable UNT0028 // Use non-allocating physics APIs
        Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, 5f, connectorLayer);
        #pragma warning restore UNT0028 // Use non-allocating physics APIs

        //we want this to be the minimal of our nearbyColliders
        Collider closestCollider;

        //quick check to ensure no bounds errors
        if (nearbyColliders.Length == 0) return;

        //simply take the collider at the closest index - order the return and take first
        else closestCollider = nearbyColliders.OrderBy
        (col => Vector3.Distance(hit.point, col.transform.position)).First();
        
        //get the closest collider from our raycast - this is the one on our target
        Connector closestConnector = closestCollider.GetComponent<Connector>();

        if (closestConnector == null) return;

        if (closestCollider != null && closestConnector != null) 
            targetConnector = closestConnector;

        //get all of our ghost elements connector elements
        Connector[] ghostConnectors = ghostBuildGameObject.GetComponentsInChildren<Connector>();

        //we need some logic here to check for a horizontal target...
        if(Mathf.Abs(targetConnector.transform.up.y) < 0.5f)
        {
            //Debug.Log("Current pillar is horizontal");

            foreach (var gc in ghostBuildGameObject.GetComponentsInChildren<Connector>())
                if (gc.connectorPosition != ConnectorPosition.top ||
                    gc.connectorPosition != ConnectorPosition.bottom)
                    ghostConnector = gc;
            
            //we need to do some logic checking here before we snap
            if (targetConnector != null && ghostConnector != null)
            {
                SnapGhostToConnector(ghostConnector, targetConnector, false, true);

                ghostifyModel(ModelParent, ghostMaterialValid);
                isGhostInValidPosition = true;
                return;
            }
        
            else
            {
                isGhostInValidPosition = false;
                return;
            } 
        }

        //else we are just doing 2 verticals next to each other - normal logic is fine
        else
        {
            //find the appropriate connector from our ghost connectors 
            foreach (var gc in ghostConnectors)
            {
                //we use our GetOpposite function within Connector.cs to find the appropriate inverse mapping to our connector
                if (gc.connectorPosition == Connector.GetOpposite(targetConnector.connectorPosition))
                {
                    //if our ghosts connector is valid - make it our gc 
                    if (gc != null)
                    {
                        ghostConnector = gc;
                        break;
                    }

                    //else somethings wrong and we need to leave
                    else
                    {
                        isGhostInValidPosition = false;
                        return;
                    }
                }
            }

            //we need to do some logic checking here before we snap
            if (targetConnector != null && ghostConnector != null)
            {
                SnapGhostToConnector(ghostConnector, targetConnector, false, false);

                ghostifyModel(ModelParent, ghostMaterialValid);
                isGhostInValidPosition = true;
                return;
            }
        
            else
            {
                isGhostInValidPosition = false;
                return;
            } 
        }
    }

    private void SetConnectorHorizontal(RaycastHit hit)
    {
        //all connectors near our raycast  - this stuff with UNT stuff was suggested by intellisense
        #pragma warning disable UNT0028 // Use non-allocating physics APIs
        Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, 5f, connectorLayer);
        #pragma warning restore UNT0028 // Use non-allocating physics APIs

        Collider closestCollider;

        //quick check to ensure no bounds errors
        if (nearbyColliders.Length == 0) return;

        //order the return and take first
        else closestCollider = nearbyColliders.OrderBy
        (col => Vector3.Distance(hit.point, col.transform.position)).First();

        //get the closest collider from our raycast - this is the one on our target
        Connector closestConnector = closestCollider.GetComponent<Connector>();

        //Debug.Log($"Closest connector to raycast: {closestConnector}");

        if (closestConnector == null) return;
        
        //only connectors we cannot connect to are top and bottom, return if these are the snapped points
        if (closestConnector.connectorPosition != ConnectorPosition.top && 
            closestConnector.connectorPosition != ConnectorPosition.bottom)
                {targetConnector = closestConnector;}

        else return;
        
        Connector[] ghostConnectors = ghostBuildGameObject.GetComponentsInChildren<Connector>();

        //same as vertical - only now we use a different function specifically built for horizontal case 
        foreach (var gc in ghostConnectors)
        {
            if (gc.connectorType != ConnectorType.Horizontal || gc == null)
                continue;

            else if (Connector.IsValidHorizontalAttachment(gc.connectorPosition, targetConnector.connectorPosition))
            {
                ghostConnector = gc;
                break;
            }
        }

        //we need to do some logic checking here before we snap
        if (targetConnector != null && ghostConnector != null)
        {
            SnapGhostToConnector(ghostConnector, targetConnector, true, false);

            ghostifyModel(ModelParent, ghostMaterialValid);
            isGhostInValidPosition = true;
            return;
        }
        
        else
        {
            isGhostInValidPosition = false;
            return;
        }
    }

    private void TrySnapToConnector(SelectedBuildType currentBuildType, RaycastHit hit)
    {
        targetConnector = null;
        ghostConnector = null;

        switch(currentBuildType)
        {
            //Vertical
            case SelectedBuildType.VerticalPillar:
                SetConnectorVertical(hit);

            break;

            //Horizontal
            case SelectedBuildType.HorizontalPillar:
                SetConnectorHorizontal(hit);

            break;

            //DefenseObject
            case SelectedBuildType.DefenseObject:
                //defense object uses the same logic as vertical, so we will resuse the function
                SetConnectorVertical(hit);
                
            break;
        }
    }

    private void AttachJointToNearbyConnector(GameObject placedObj, float force, float torque)
    {
        //we set this continously within the prefab to raycast function above
        //we can use this to joint the most recently updated target to the new object 
        Rigidbody targetRb = targetConnector.GetComponentInParent<Rigidbody>();
        Rigidbody placedRb = placedObj.GetComponent<Rigidbody>();

        if (targetRb != null && placedRb != null && targetRb != placedRb)
        {
            //Debug.Log($"Attached to Connector. Target Rb: {targetRb} | Placed Rb {placedRb}");
            FixedJoint joint = placedObj.AddComponent<FixedJoint>();
            joint.connectedBody = targetRb;
            joint.breakForce = force;
            joint.breakTorque = torque;
        }
    }

    private void AttachToConnector(SelectedBuildType currentBuildType, GameObject newBuild)
    /*
    The same structure as AttackManager, a case dispatcher tied to 3 individual logic blocks
    This is not clean and sleek, but legible and sensible. I prefer simplicity and interpretability to 
    advanced structures or minimalist syntax.
    */
    {
        //depending on type of block - we may want specific checks before
        switch (currentBuildType)
        {
            case SelectedBuildType.VerticalPillar:
                AttachJointToNearbyConnector(newBuild, breakForce, breakTorque);

                DestroyGhost();

            break;

            case SelectedBuildType.HorizontalPillar:
                AttachJointToNearbyConnector(newBuild, breakForce, breakTorque);

                DestroyGhost();

            break;

            case SelectedBuildType.DefenseObject:
                if (defenseObjectPlaced)
                {
                    DestroyGhost();
                    Destroy(newBuild);
                    return;
                }

                else defenseObjectPlaced = true;

                AttachJointToNearbyConnector(newBuild, 1000f, 1000f);

            break;
        }
    }

    private void PlaceBuild()
    {
        if (ghostBuildGameObject == null || !isGhostInValidPosition) return;

        else
        {
            //we are trying to place an object at the ghost position, so we need to get the ghost objects placement rotation etc 
            ghostBuildGameObject.transform.GetPositionAndRotation(out Vector3 placePos, out Quaternion placeRot);

            //instantiate the current build according to the selected build and get the rigidbody
            GameObject newBuild = Instantiate(currentBuild, placePos, placeRot);
            Rigidbody rb = newBuild.GetComponent<Rigidbody>();

            //save this for clean up when round is over
            placedObjects.Add(newBuild);

            //Connector[] conns = newBuild.GetComponentsInChildren<Connector>();
            //foreach (var c in conns)
                //c.isOccupied = true;

            //an unnecessary safety check to make sure we set this as a physical object 
            //since ghosts are kinematic, it is a good idea to sanity check this here 
            if (rb != null)
            {
                //call our dispatcher to attach our newly minted object to a connector
                AttachToConnector(currentBuildType, newBuild);

                //make sure these are set properly so the attacker can actually destroy the structure
                rb.isKinematic = false;
                rb.useGravity = true; 
                rb.detectCollisions = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
    }
}