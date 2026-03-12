using UnityEngine;
using System.Collections;

public class LookAt : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform lookAtTransform;
    [SerializeField] private float characterRotationDuration = 0.2f;

    public IEnumerator RotateBothToFaceEachOther()
    {
        if (playerTransform == null || lookAtTransform == null)
            yield break;

        float duration = Mathf.Max(0.01f, characterRotationDuration);
        float elapsed = 0f;

        Quaternion startPlayerRot = playerTransform.rotation;
        Vector3 playerDir = lookAtTransform.position - playerTransform.position;
        playerDir.y = 0f;
        Quaternion targetPlayerRot = playerDir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(playerDir.normalized) : startPlayerRot;

        Transform camTransform = null;
        Quaternion startCamLocalRot = Quaternion.identity;
        Quaternion targetCamLocalRot = Quaternion.identity;
        PlayerController playerController = playerTransform.GetComponent<PlayerController>();

        var cinemachineCam = playerTransform.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        if (cinemachineCam != null)
            camTransform = cinemachineCam.transform;

        if (camTransform != null)
        {
            startCamLocalRot = camTransform.localRotation;
            Vector3 toNpc = lookAtTransform.position - camTransform.position;
            Vector3 flatForward = playerDir.normalized;
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = playerTransform.forward;
            float pitchAngle = -Mathf.Atan2(toNpc.y - 0f, new Vector3(toNpc.x, 0f, toNpc.z).magnitude) * Mathf.Rad2Deg;
            pitchAngle = Mathf.Clamp(-Mathf.Asin(toNpc.normalized.y) * Mathf.Rad2Deg, -80f, 80f);
            targetCamLocalRot = Quaternion.Euler(pitchAngle, 0f, 0f);
        }

        Quaternion startOtherRot = lookAtTransform.rotation;
        Vector3 otherDir = playerTransform.position - lookAtTransform.position;
        otherDir.y = 0f;
        Quaternion targetOtherRot = otherDir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(otherDir.normalized) : startOtherRot;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            playerTransform.rotation = Quaternion.Slerp(startPlayerRot, targetPlayerRot, t);
            lookAtTransform.rotation = Quaternion.Slerp(startOtherRot, targetOtherRot, t);

            if (camTransform != null)
                camTransform.localRotation = Quaternion.Slerp(startCamLocalRot, targetCamLocalRot, t);

            yield return null;
        }

        playerTransform.rotation = targetPlayerRot;
        lookAtTransform.rotation = targetOtherRot;

        if (camTransform != null)
            camTransform.localRotation = targetCamLocalRot;
    }
}
