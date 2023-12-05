using TOHE;
using UnityEngine;

public class MoveWithMouse : MonoBehaviour
{
    private Collider2D selectedCollider;
    private bool isTeleporting;

    public void Update()
    {
        if (Main.AllowTPs.Value == true && PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsMod)
        {

        if (Input.GetMouseButtonDown(0))
        {
            SelectObject();
        }

        if (Input.GetMouseButtonDown(2))
        {
        selectedCollider = null;
        }

        if (Input.GetMouseButtonDown(1))
        {
            isTeleporting = true;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isTeleporting = false;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
        selectedCollider.transform.SetParent(null);
        DontDestroyOnLoad(selectedCollider.gameObject);
        }

        if (Input.GetKeyDown(KeyCode.Insert))
        {
        Destroy(selectedCollider.gameObject);
        }
    }
}

    public void FixedUpdate()
    {
        if (isTeleporting && selectedCollider != null)
        {
            TeleportObject();
        }
    }

    void SelectObject()
    {
        if (GameObject.Find("LeftstickDeadzone") != null)
        {
        GameObject.Find("LeftstickDeadzone").SetActive(false);
        GameObject.Find("RightstickDeadzone").SetActive(false);
        }
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition);

        if (hitCollider != null)
        {
            if (!hitCollider.GetComponent<PlayerControl>()) { return; }
            selectedCollider = hitCollider;
                Debug.Log(selectedCollider.name);
        }
    }

    void TeleportObject()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        selectedCollider.GetComponent<CustomNetworkTransform>().RpcSnapTo(mousePosition);
    }
}