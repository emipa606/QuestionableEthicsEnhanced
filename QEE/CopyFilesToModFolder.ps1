Param(
    #Relative filepath to the folder in the local rimworld 'Mods' folder to use for output. 
    #The default value uses the default Rimworld installation path
    [string]$localModDir = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\QEE"
)

function Copy-Modfiles()
{
    $switches = @("/MIR", "/COPY:DAT", "/XO", "/NP", "/R:5", "/W:5")
    If (Test-Path "..\Textures") {
        Robocopy.exe "..\About" "$localModDir\About" *.* $switches
        Robocopy.exe "..\Languages" "$localModDir\Languages" *.* $switches
        Robocopy.exe "..\Textures" "$localModDir\Textures" *.* $switches
        Robocopy.exe "..\1.0" "$localModDir\1.0" *.* $switches
        Robocopy.exe "..\1.1" "$localModDir\1.1" *.* $switches
    }
    Else {
        Write-Output "Mod sub-directories not found! Verify your current Powershell directory is the mod project folder"
    }

}
Copy-Modfiles