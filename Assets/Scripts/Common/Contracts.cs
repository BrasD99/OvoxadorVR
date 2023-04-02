using System.Collections;
using System.Collections.Generic;

public class Dump
{
    public List<float[]> Cameras { get; set; }
    public Dictionary<int, Frame> Frames { get; set; }
}

public class Frame
{
    public float[] Ball_position { get; set; }
    public List<Player> Players { get; set; }
}

public class Player
{
    public int Id { get; set; }
    public int Camera_id { get; set; }
    public PoseDump Pose { get; set; }
    public List<float> Position { get; set; }
}

public class PoseDump
{
    public List<float[][]> Verts { get; set; }
    public List<float[][][]> Pose { get; set; }
    public List<float[]> Betas { get; set; }
    public List<float[][]> Joints3d { get; set; }
    public List<float[][]> Smpl_joints2d { get; set; }
}

public class BallPositionCommon: BallPosition
{
    public NextBallPosition NextBallPosition { get; set; }
}

public class NextBallPosition: BallPosition
{
    public int NextFrameId { get; set; }
}

public class BallPosition
{
    public float[] BallPositionData { get; set; }
}