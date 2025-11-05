using UnityEngine;

public class RaceCar
{
    private const float Max = 1;
    private long timestamp;
    public float throttle = 0f;
    public float steering = 0f;
    public float headPanDeg = 0f;   // yaw delta in degrees, [-90..90] after clamp
    public float headTiltDeg = 0f;  // pitch delta in degrees, [-45..45] after clamp
    public bool headMove = false;

    public long Timestamp
    {
        get => timestamp;
        set => timestamp = value;
    }

    public float Throttle
    {
        get { return throttle; }
        set
        {
            if ((value > 0 && throttle < 0) || (value < 0 && throttle > 0))
            {
                throttle = 0;
                return;
            }
            throttle = value > Max ? Max : (value < -Max ? -Max : value);
        }
    }

    public float Steering
    {
        get { return steering; }
        set
        {
            if ((value > 0 && steering < 0) || (value < 0 && steering > 0))
            {
                steering = 0;
                return;
            }
            steering = value > Max ? Max : (value < -Max ? -Max : value);
        }
    }
}
