using UnityEngine;


public class RaceCar
{
    private const float Max = 1;
    private long timestamp;
    public float throttle = 0;

    public long Timestamp
    {
        get => timestamp;
        set => timestamp = value;
    }

    public float steering = 0;

    public float Throttle
    {
        get { return throttle; }
        set
        {
            if (value > 0 && throttle < 0 || value < 0 && throttle > 0)
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
            if (value > 0 && steering < 0 || value < 0 && steering > 0)
            {
                steering = 0;
                return;
            }

            steering = value > Max ? Max : (value < -Max ? -Max : value);
        }
    }
}
