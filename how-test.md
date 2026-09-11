 2. Enable hologram interception

  After launching once, close Valheim. Open:

  C:\Users\magni\AppData\Roaming\r2modmanPlus-
  local\Valheim\profiles\CatosBuildHologram\BepInEx\config\com.catosaur.catosbuildhologram.client.cfg

  Change these values:

  [Blueprints]

  EnableBlueprintInterception = true

  [Diagnostics]

  DebugLogging = true

  [Interface]

  ShowStatusHud = true

  Leave these disabled:

  EnablePredictedSupport = false
  EnableNativeInputAssist = false

  Restart the client and reconnect.

  3. Test basic hologram placement

  In-game:

  1. Equip the hammer.
  2. Select a normal building piece.
  3. Aim at a valid placement location until the normal Valheim preview is valid/green.
  4. Click to place.

  Expected result:

  - The real piece does not appear.
  - Materials and stamina are not consumed.
  - A hologram appears at the intended location.
  - The top-left HUD shows the local plan and hologram count increasing.
  - The hologram will currently appear gray because support status is still Unknown.

  Test an invalid placement as well. Clicking while the native preview is invalid should not create a hologram or consume materials.

  4. Test persistence

  After creating one or more holograms:

  1. Exit to the main menu.
  2. Close Valheim completely.
  3. Relaunch the same r2modman profile.
  4. Reconnect to the test server.

  The local holograms should reload from:

  C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosBuildHologram\BepInEx\config\CatosBuildHologram.localplans.json

  5. Current limitations

  Do not expect green/red support colors yet. The renderer supports those colors, but the server-side native support resolver and
  synchronization are not implemented.

  Also, F8 currently selects the next blueprint’s native piece, but guided real-piece completion is not fully testable yet because
  blueprint interception still catches the subsequent native placement. That needs a one-shot bypass before guided construction can be
  verified end-to-end.

  Useful logs:

  Client:
  ...\CatosBuildHologram\BepInEx\LogOutput.log

  Server:
  C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server\BepInEx\LogOutput.log

  For now, the meaningful test is: valid native preview → click → local gray hologram, no real build, no resource consumption, and
  persistence after restart.