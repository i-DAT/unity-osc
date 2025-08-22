using UnityEngine;

// public class RotateObject : MonoBehaviour
// {
//     void Start()
//     {
//         GameObject.Find("OSC Manager").GetComponent<OSCManager>().Handle("/rotation", Rotation);
//     }

//     void Rotation(OSCMessage msg)
//     {
//         float x = msg.args[0] * Mathf.Rad2Deg;
//         float y = msg.args[1] * Mathf.Rad2Deg;
//         float z = msg.args[2] * Mathf.Rad2Deg;
//         transform.rotation = Quaternion.Euler(-x, z - 90, -y);
//     }
// }
