using System;

namespace MonteApp.ApiService.Models;

public static class Constants
{
    public const string HomeUrl = "https://montessori.bo";
    public const string BaseUrl = $"{HomeUrl}/principal/public";
    // public const string BaseUrl = "https://montessori.bo/principal/public";
    public const string LoginUrl = $"{BaseUrl}/login";
    public const string LogoutUrl = $"{BaseUrl}/logout";
    public const string SistemaPadresId = "2";
    public const string LoginPadresUrl = $"{LoginUrl}?sistema={SistemaPadresId}";
    public const string PadresUrl = $"{BaseUrl}/padres";
    public const string SubsysControlSemanalUrl = $"{BaseUrl}/control_semanal";
    public const string SubsysLicenciasUrl = $"{HomeUrl}/LicenciasP";
    public const string SubsysLicencias_LicenciasAlumnosUrl = $"{SubsysLicenciasUrl}/licencias_alumnos.php?id=";
    public const string SubsysLicencias_LicenciaEnviaUrl = $"{SubsysLicenciasUrl}/licencia_envia.php"; // TODO: This is not used, but it might be useful in the future
}
