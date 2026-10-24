namespace EAktePrototypProjektNewMvc.Helpers
{
    // Diese Erweiterungsmethoden ermöglichen das Speichern und Laden von komplexen Objekten in der Session,
    // indem sie in JSON serialisiert und beim Abrufen wieder deserialisiert werden.
    public static class SessionExtensions
    {
        // SetObject serialisiert das Objekt in JSON und speichert es in der Session.
        public static void SetObject(this ISession session, string key, object value)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            session.SetString(key, json);
        }

        // GetObject deserialisiert den JSON-String wieder zurück in das ursprüngliche Objekt.
        public static T GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return json == null ? default(T) : System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
    }
}
