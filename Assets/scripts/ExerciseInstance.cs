public class ExerciseInstance
{
    public ExerciseData Data { get; private set; }

    public int HitCount { get; private set; }

    public bool IsFinished { get; private set; }

    public ExerciseInstance(ExerciseData data)
    {
        Data = data;
        HitCount = 0;
        IsFinished = false;
    }

    public void RegisterHit()
    {
        HitCount++;
    }

    public void Reset()
    {
        HitCount = 0;
        IsFinished = false;
    }

    public void Finish()
    {
        IsFinished = true;
    }

    public void Finish(int finalHitCount)
    {
        HitCount = finalHitCount;
        IsFinished = true;
    }
}
