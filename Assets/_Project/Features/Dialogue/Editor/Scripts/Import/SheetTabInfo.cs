using System;

namespace MyGame.Features.Dialogue.Editor
{
    [Serializable]
    public class SheetTabInfo
    {
        public string Title;
        public int SheetId;
        public bool Selected = true;
        public int EntryCount;

        public string GetExportURL(string spreadsheetId)
        {
            return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={SheetId}";
        }
    }
}
