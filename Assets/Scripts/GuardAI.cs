using UnityEngine;
using FOV;

public class GuardAI : MonoBehaviour
{
    private FieldOfView fov;

    void Start()
    {
        fov = GetComponent<FieldOfView>();
    }

    void Update()
    {
        // Example: looking for the player's Transform component
        var detectedTargets = fov.Field<Transform>("Player");

        if (detectedTargets.Count > 0)
        {
            Debug.Log("I SEE YOU: " + detectedTargets[0].name);
            // Chase, attack, etc.
        }
    }
}