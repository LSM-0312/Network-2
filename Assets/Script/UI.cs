using Fusion;
using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private TMP_Text roleText;

    private NetworkRunner _runner;
    private PlayerAvatar _localAvatar;
    private PlayerRole _lastRole = (PlayerRole)(-1);

    private void Update()
    {
        if (roleText == null)
            return;

        if (_runner == null)
            _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner == null || !_runner.IsRunning)
        {
            roleText.text = "";
            return;
        }

        if (_localAvatar == null)
        {
            if (_runner.TryGetPlayerObject(_runner.LocalPlayer, out NetworkObject playerObj))
                _localAvatar = playerObj.GetComponent<PlayerAvatar>();
        }

        if (_localAvatar == null)
        {
            roleText.text = "";
            return;
        }

        if (!_localAvatar.HasInputAuthority)
        {
            roleText.text = "";
            return;
        }

        if (_localAvatar.Role == _lastRole)
            return;

        _lastRole = _localAvatar.Role;

        switch (_localAvatar.Role)
        {
            case PlayerRole.Cop:
                roleText.text = "You are a COP";
                break;

            case PlayerRole.Robber:
                roleText.text = "You are a ROBBER";
                break;

            default:
                roleText.text = "";
                break;
        }
    }
}