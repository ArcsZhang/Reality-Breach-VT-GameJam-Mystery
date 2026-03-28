using System;

using UnityEngine;
public class FollowCamera : MonoBehaviour

{
    public Transform player;
    public Vector3 offset = new Vector3(0, 0, -10);

    void LateUpdate()
    {
        Debug.Log("Camera is updating to: " + player.position);
        Vector3 target = player.position + offset;
        transform.position = target;
    }
}
