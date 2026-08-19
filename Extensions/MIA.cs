namespace Extensions
{
    public static class MIA
    {
        public enum MIAUpdateType
        {
            Regular = 0,
            doUpdate = 1,
            forceUpdate = 2
        }

        public static MIAUpdateType updateType = MIAUpdateType.Regular;

        public delegate void ShowTitleMessage(string message);

        public static ShowTitleMessage stme = null;

        public static void UpdateMIA()
        {
        }

        public static void UpdateMIA(object worker)
        {
        }


        public static void SendMIAFound(object miaFound)
        {
        }


        public static void ClearOut()
        {
        }
    }
}
