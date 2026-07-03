public static class InfiniteBattleRunContext
{
    public static string ClearId { get; private set; }
    public static int ClearNumber { get; private set; }
    public static GameClearRecordData Record { get; private set; }
    public static ClearRecordPlayerProfile Profile { get; private set; }
    public static int CurrentFloor { get; private set; }
    public static int HighestReachedFloor { get; private set; }
    public static bool IsRunPrepared { get; private set; }

    public static void Prepare(GameClearRecordData record, ClearRecordPlayerProfile profile)
    {
        Clear();

        Record = record;
        Profile = profile;
        ClearId = record != null ? record.clearId : "";
        ClearNumber = record != null ? record.clearNumber : 0;
        CurrentFloor = 1;
        HighestReachedFloor = 0;
        IsRunPrepared = record != null && profile != null;
    }

    public static void Clear()
    {
        if (Profile != null)
            Profile.Dispose();

        ClearId = "";
        ClearNumber = 0;
        Record = null;
        Profile = null;
        CurrentFloor = 0;
        HighestReachedFloor = 0;
        IsRunPrepared = false;
    }
}
