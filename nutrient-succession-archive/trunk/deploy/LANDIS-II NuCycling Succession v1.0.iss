#define PackageName      "Nutrient Cycling Succession"
#define PackageNameLong  "Nutrient Cycling Succession Extension"
#define Version          "1.0"
#define ReleaseType      "official"
#define ReleaseNumber    "1"

#define CoreVersion      "5.1"
#define CoreReleaseAbbr  ""

#include AddBackslash(GetEnv("LANDIS_DEPLOY")) + "package (Setup section).iss"

#if ReleaseType != "official"
  #define Configuration  "debug"
#else
  #define Configuration  "release"
#endif

[Files]
; Auxiliary libs
Source: {#LandisBuildDir}\Landis.Library.Cohorts.LeafBiomass.dll; DestDir: {app}\bin; Flags: replacesameversion
Source: {#LandisBuildDir}\Landis.Succession.dll; DestDir: {app}\bin; Flags: uninsneveruninstall replacesameversion
Source: {#LandisBuildDir}\Troschuetz.Random.dll; DestDir: {app}\bin; Flags: uninsneveruninstall replacesameversion

; Nutrient Cycling Succession
Source: {#LandisBuildDir}\Landis.Extension.Succession.NuCycling.dll; DestDir: {app}\bin; Flags: replacesameversion
; Source: docs\LANDIS-II Century Succession v1.0 User Guide.pdf; DestDir: {app}\docs
; Source: examples\*; DestDir: {app}\examples\century-succession

#define NuCSucc "Nutrient Cycling Succession 1.0.txt"
Source: {#NuCSucc}; DestDir: {#LandisPlugInDir}

[Run]
;; Run plug-in admin tool to add an entry for the plug-in
#define PlugInAdminTool  CoreBinDir + "\Landis.PlugIns.Admin.exe"

Filename: {#PlugInAdminTool}; Parameters: "remove ""Nutrient Cycling Succession"" "; WorkingDir: {#LandisPlugInDir}
Filename: {#PlugInAdminTool}; Parameters: "add ""{#NuCSucc}"" "; WorkingDir: {#LandisPlugInDir}

[UninstallRun]
;; Run plug-in admin tool to remove the entry for the plug-in
; Filename: {#PlugInAdminTool}; Parameters: "remove ""Century Succession"" "; WorkingDir: {#LandisPlugInDir}

[Code]
#include AddBackslash(LandisDeployDir) + "package (Code section) v3.iss"

//-----------------------------------------------------------------------------

function CurrentVersion_PostUninstall(currentVersion: TInstalledVersion): Integer;
begin
  // Do not remove version 1.0 from the database.
  //if StartsWith(currentVersion.Version, '1') then
  //  begin
  //    Exec('{#PlugInAdminTool}', 'remove "Century Succession"',
  //         ExtractFilePath('{#PlugInAdminTool}'),
  //		   SW_HIDE, ewWaitUntilTerminated, Result);
//	end
  //else
    Result := 0;
end;

//-----------------------------------------------------------------------------

function InitializeSetup_FirstPhase(): Boolean;
begin
  CurrVers_PostUninstall := @CurrentVersion_PostUninstall
  Result := True
end;
