using System.Runtime.InteropServices;

using Newtonsoft.Json;

using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace BAAZ.CMMS.App.Services;

public sealed class WindowsCredentialSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string Resource = "BAAZ.CMMS.Supabase.Session";
    private const string Username = "default";

    public void DestroySession()
    {
        try
        {
            DevWinUI.CredentialHelper.RemovePasswordCredential(Resource, Username);
        }
        catch (COMException ex) when (IsCredentialNotFound(ex))
        {
            // Учётных данных ещё нет — нечего удалять.
        }
    }

    public Session? LoadSession()
    {
        try
        {
            var credential = DevWinUI.CredentialHelper.GetPasswordCredential(Resource, Username);
            if (credential is null || string.IsNullOrWhiteSpace(credential.Password))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Session>(credential.Password);
        }
        catch
        {
            return null;
        }
    }

    public void SaveSession(Session session)
    {
        var serialized = JsonConvert.SerializeObject(session);
        DevWinUI.CredentialHelper.AddPasswordCredential(Resource, Username, serialized);
    }

    private static bool IsCredentialNotFound(COMException ex) => (uint)ex.HResult == 0x80070490;
}
