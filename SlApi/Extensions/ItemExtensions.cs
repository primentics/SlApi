namespace SlApi.Extensions
{
    public static class ItemExtensions
    {
        public static bool IsScpItem(this ItemType type)
            => type is ItemType.SCP018 || type is ItemType.SCP1576 || type is ItemType.SCP1853 || type is ItemType.SCP207 
            || type is ItemType.SCP2176 || type is ItemType.SCP244a || type is ItemType.SCP244b || type is ItemType.SCP268 
            || type is ItemType.SCP330 || type is ItemType.SCP500;
    }
}
