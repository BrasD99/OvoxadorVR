using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using System.Linq;

public class FramesEnumerator
{
    private readonly IEnumerator _enumerator;
    public FramesEnumerator(string folder)
    {
        _enumerator = new FramesLoader(folder);
    }
    public IEnumerator GetEnumerator() => _enumerator;
}

public class FramesLoader : IEnumerator
{
    private readonly string _folder;
    private List<float[]> _cameras;
    private List<Frame> _frames;
    private int _framePosition = -1;
    private int _realFramePosition = 0;
    private int _dumpPosition = 0;

    public FramesLoader(string folder)
    {
        _folder = folder;
        _frames = new List<Frame>();
        LoadDump();
    }

    private bool LoadDump(int num = 0)
    {
        var dumpFileLocation = Path.Combine(_folder, $"{num}.json");
        Debug.Log($"Loading dump: {dumpFileLocation}...");
        if (File.Exists(Path.Combine(_folder, dumpFileLocation)))
        {
            using (var streamReader = new StreamReader(dumpFileLocation))
            {
                var dumpFile = streamReader.ReadToEnd();
                var dump = JsonConvert.DeserializeObject<Dump>(dumpFile);
                _frames = dump.Frames.Values.ToList();
                _cameras = dump.Cameras;
                _framePosition = 0;
            }

            Debug.Log($"Dump: {num}.json loaded");

            return true;
        }

        Debug.Log($"Dump: {num}.json not exist");

        return false;
    }

    private BallPositionCommon GetBallPositionCommon()
    {
        var ballPosition = _frames[_framePosition].Ball_position;
        var commonPosition = new BallPositionCommon
        {
            BallPositionData = ballPosition
        };

        for (int i = _framePosition; i < _frames.Count; i++)
        {
            if (_frames[i].Ball_position != null)
            {
                commonPosition.NextBallPosition = new NextBallPosition
                {
                    BallPositionData = _frames[i].Ball_position,
                    NextFrameId = i + _realFramePosition
                };

                break;
            }
        }

        return commonPosition;
    }

    public object Current
    {
        get
        {
            if (_framePosition == -1)
                throw new ArgumentException();

            var ballCommonPosition = GetBallPositionCommon();

            return (_cameras, _frames[_framePosition], ballCommonPosition);
        }
    }

    public bool MoveNext()
    {
        _framePosition++;
        _realFramePosition++;
        if (_framePosition >= _frames.Count)
        {
            _dumpPosition++;
            if (LoadDump(_dumpPosition))
            {
                return _frames.Count > _framePosition;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public void Reset()
    {
        _framePosition = 0;
        _dumpPosition = 0;
    }
}
