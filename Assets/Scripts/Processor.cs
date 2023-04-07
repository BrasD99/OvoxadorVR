using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Processor : MonoBehaviour
{
    public GameObject Player;
    public GameObject Ball;
    public Material PlayerMaterial;
    public string ProjectFolder = "/Users/brasd99/Desktop/Dissertation/outputs/project";

    private static IEnumerator _enumerator;
    private bool _isRunning = true;
    private float _waitTime = 0.1f;
    private float _timer = 0.0f;
    private bool _isWaiting = false;
    private Dictionary<int, GameObject> _playerObjects;
    private static readonly string[] _boneNames = new string[]
    {
        "Pelvis",
        "L_Hip",
        "R_Hip",
        "Spine1",
        "L_Knee",
        "R_Knee",
        "Spine2",
        "L_Ankle",
        "R_Ankle",
        "Spine3",
        "L_Foot",
        "R_Foot",
        "Neck",
        "L_Collar",
        "R_Collar",
        "Head",
        "L_Shoulder",
        "R_Shoulder",
        "L_Elbow",
        "R_Elbow",
        "L_Wrist",
        "R_Wrist",
        "L_Hand",
        "R_Hand"
    };
    private float _pitchWidth;
    private float _pitchHeight;
    private Dictionary<int, Vector3> _ballPositions;
    private int _frameId = 0;

    // Start is called before the first frame update
    void Start()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-projectLocation")
            {
                ProjectFolder = args[i + 1];
                Debug.Log($"Project folder passed: {ProjectFolder}");
                break;
            }
        }

        if (string.IsNullOrEmpty(ProjectFolder))
            ProjectFolder = "/Users/brasd99/Desktop/Dissertation/outputs/project";

        (_pitchWidth, _pitchHeight) = LoadPitch();
        var dumpsFolder = Path.Combine(ProjectFolder, "dumps");
        var framesEnumerator = new FramesEnumerator(dumpsFolder);
        _enumerator = framesEnumerator.GetEnumerator();
        _playerObjects = new Dictionary<int, GameObject>();
        _ballPositions = new Dictionary<int, Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isWaiting)
        {
            if (_isRunning && _enumerator.MoveNext())
            {
                var frameData = _enumerator.Current as (List<float[]>, Frame, BallPositionCommon)?;
                if (frameData.HasValue)
                {
                    var cameraPositions = frameData.Value.Item1;
                    var frame = frameData.Value.Item2;
                    var ballPositionCommon = frameData.Value.Item3;

                    foreach (var player in frame.Players)
                    {
                        var playerObject = GetPlayerObject(player);
                        UpdateModel(playerObject, player, cameraPositions);
                    }

                    //ChangePlayersVisibility(frame.Players);

                    var ballPosition = GetInterpolateBallPosition(ballPositionCommon, Ball.transform.position);
                    if (ballPosition.HasValue)
                    {
                        UpdateBall(ballPosition.Value);
                    }
                }
            }
            else
            {
                _isRunning = false;
            }

            _isWaiting = true;
            _timer = 0.0f;
        }
        else
        {
            _timer += Time.deltaTime;
            if (_timer >= _waitTime)
                _isWaiting = false;
        }
    }

    #region Helper methods
    private void ChangePlayersVisibility(List<Player> players)
    {
        var notFoundPlayersIds = new List<int>();

        foreach (var playerId in _playerObjects.Keys)
        {
            var player = players.FirstOrDefault(u => u.Id == playerId);

            if (player != null)
            {
                var playerObject = _playerObjects[playerId];
                var renderer = playerObject.GetComponent<Renderer>();
                renderer.enabled = true;
                playerObject.SetActive(true);
            }
            else
            {
                notFoundPlayersIds.Add(playerId);
            }
        }

        foreach(var playerId in notFoundPlayersIds)
        {
            var playerObject = _playerObjects[playerId];
            var renderer = playerObject.GetComponent<Renderer>();
            renderer.enabled = false;
            playerObject.SetActive(false);
        }
    }

    private Vector3? GetInterpolateBallPosition(BallPositionCommon ballPosition, Vector3 currentPosition)
    {
        if (_ballPositions.ContainsKey(_frameId))
        {
            return _ballPositions[_frameId];
        }

        if (ballPosition.BallPositionData != null)
        {
            _ballPositions.Clear();
            var currentBallPosition = new Vector3(ballPosition.BallPositionData[0], ballPosition.BallPositionData[1]);
            if (ballPosition.NextBallPosition != null)
            {
                for (int i = _frameId + 1; i < ballPosition.NextBallPosition.NextFrameId; i++)
                {
                    var nextBallPosition = new Vector3(ballPosition.NextBallPosition.BallPositionData[0], ballPosition.NextBallPosition.BallPositionData[1]);
                    var interpolated = Vector3.Lerp(currentBallPosition, nextBallPosition, i / ballPosition.NextBallPosition.NextFrameId);
                    _ballPositions.Add(i, interpolated);
                }
            }

            return currentBallPosition;
        }

        return null;
    }
    private (float, float) LoadPitch()
    {
        var pitchImageFileLocation = Path.Join(ProjectFolder, "textures/pitch.jpg");
        var imageBytes = File.ReadAllBytes(pitchImageFileLocation);

        // Create a Texture2D from the byte array
        var texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        // Create a sprite from the texture
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        // Get the SpriteRenderer component on the GameObject
        var renderer = GetComponent<SpriteRenderer>();

        // Set the sprite for the SpriteRenderer component
        renderer.sprite = sprite;

        var pitchWidth = renderer.bounds.size.x;
        var pitchHeight = renderer.bounds.size.y;

        // Set the height and width of the sprite to the same value
        float size = Mathf.Max(pitchWidth, pitchHeight);
        renderer.size = new Vector2(size, size);

        return (pitchWidth, pitchHeight);
    }

    private float[] MapCoordinates(float[] input)
    {
        (var x, var y) = (input[0], input[1]);
        (var centerX, var centerY) = (_pitchWidth / 2, _pitchHeight / 2);

        if(x >= centerX)
        {
            x = (x - centerX) / 100;
        }
        else
        {
            x = - (centerX - x) / 100;
        }

        if(y >= centerY)
        {
            y = - (y - centerY) / 100;
        }
        else
        {
            y = (centerY - y) / 100;
        }

        return new float[2] { x, y };
    }

    private Material LoadPlayerTexture(int playerId)
    {
        var playerTextureLocation = Path.Join(ProjectFolder, $"textures/player_{playerId}.png");
        var imageBytes = File.ReadAllBytes(playerTextureLocation);
        // Create a Texture2D from the byte array
        var texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        Material material = Instantiate(PlayerMaterial);

        // Set the texture to the Albedo property of the material
        material.SetTexture("_MainTex", texture);

        return material;
    }

    private GameObject GetPlayerObject(Player player)
    {
        if (!_playerObjects.ContainsKey(player.Id))
        {
            GameObject playerObject = (GameObject)Instantiate(Player);
            playerObject.transform.SetParent(transform);
            playerObject.transform.localScale = Player.transform.localScale;

            var material = LoadPlayerTexture(player.Id);

            var playerAvg = FindChildGameObject(playerObject, "m_avg");

            // Get the SkinnedMeshRenderer component from the target GameObject
            var skinnedMeshRenderer = playerAvg.GetComponent<SkinnedMeshRenderer>();
            // Set the material on the SkinnedMeshRenderer component
            skinnedMeshRenderer.material = material;

            _playerObjects.Add(player.Id, playerObject);

            return playerObject;
        }
        else
        {
            return _playerObjects[player.Id];
        }
    }

    private static void RotateToCamera(GameObject player, Vector3 cameraPosition)
    {
        var direction = Vector3.Normalize(player.transform.position - cameraPosition);
        var angle = Mathf.Atan2(direction.y, direction.z) * Mathf.Rad2Deg;
        var rotation = Quaternion.Euler(angle, -90, -90);
        player.transform.rotation = rotation;
    }

    private void ApplyBetas(GameObject player, float[] betas)
    {
        var avg = FindChildGameObject(player, "m_avg");

        var meshRenderer = avg.GetComponent<SkinnedMeshRenderer>();

        for (int i = 0; i < betas.Length; i++)
        {
            meshRenderer.SetBlendShapeWeight(i, betas[i] * 10);
        }
    }

    private void ApplyPosition(GameObject player, float[] position)
    {
        var pitchPosition = MapCoordinates(position);
        player.transform.localPosition = new Vector3(pitchPosition[0], pitchPosition[1]);
    }

    private void ApplyPose(GameObject player, float[][][] pose)
    {
        for (int boneIndex = 0; boneIndex < pose.Length; boneIndex++)
        {
            var boneName = $"m_avg_{_boneNames[boneIndex]}";
            var objectBone = FindChildGameObject(player, boneName);

            var quat = Quaternion.LookRotation(
                new Vector3(-pose[boneIndex][0][2], pose[boneIndex][1][2], pose[boneIndex][2][2]),
                new Vector3(-pose[boneIndex][0][1], pose[boneIndex][1][1], pose[boneIndex][2][1])
            );

            if (boneIndex == 0)
            {
                var quat_x = Quaternion.AngleAxis(-90, new Vector3(1, 0, 0));
                var quat_z = Quaternion.AngleAxis(-90, new Vector3(0, 0, 1));
                var r = Quaternion.Euler(180, 0, 0) * quat;
                objectBone.transform.localRotation = r;
            }
            else
            {
                objectBone.transform.localRotation = quat;
            }
        }
    }

    private Vector3 GetLowerBound(GameObject bone)
    {
        var skinnedMeshRenderer = bone.GetComponent<SkinnedMeshRenderer>();
        return skinnedMeshRenderer.localBounds.min;
    }

    private void UpdateModel(GameObject player, Player playerDump, List<float[]> cameraPositions)
    {
        ApplyPosition(player, playerDump.Position.ToArray());
        ApplyPose(player, playerDump.Pose.Pose[0]);
        ApplyBetas(player, playerDump.Pose.Betas[0]);
        var cameraPosition = cameraPositions[playerDump.Camera_id];
        var cameraVectorPosition = new Vector3(cameraPosition[0], cameraPosition[1]);
        RotateToCamera(player, cameraVectorPosition);
        /*var avgBone = FindChildGameObject(player, "m_avg");
        var lowerBound = GetLowerBound(avgBone);
        var pelvis = FindChildGameObject(player, "m_avg_Pelvis");
        pelvis.transform.localPosition += new Vector3(0, lowerBound.y + 0.05f, 0);*/
    }

    private void UpdateBall(Vector3 ballPosition)
    {
        ApplyPosition(Ball, new float[] { ballPosition.x, ballPosition.y });
    }

    private GameObject FindChildGameObject(GameObject parent, string name)
    {
        var children = parent.GetComponentsInChildren<Transform>();
        return children.FirstOrDefault(u => u.gameObject.name == name)?.gameObject;
    }
    #endregion
}
