# ToasterReskinLoader Integration - Summary

## Overview
This integration allows your **Scoreboard mod to control colors for BOTH UI elements AND equipment** using a single hex color configuration, leveraging ToasterReskinLoader's color system when available.

---

## Architecture

### Current Setup (Before Integration)
```
Scoreboard Config (hex colors)
    ├→ Scoreboard Patches → UI Colors (minimap, announcements)
    └→ (ToasterReskinLoader separate) → Equipment Colors (sticks, helmets)
```

### New Setup (After Integration)
```
Scoreboard Config (hex colors)
    ├→ Scoreboard Patches → UI Colors (minimap, announcements, leaderboard)
    ├→ ToasterReskinLoaderColorSync → Equipment Colors via ToasterReskinLoader
    └→ Both systems stay in sync automatically
```

---

## Key Components Created

### 1. **ToasterReskinLoaderColorSync.cs** (Bridge Service)
- **SyncTeamColors(blueHex, redHex)** - Push Scoreboard colors to ToasterReskinLoader
- **SyncMinimapColors(puckHex)** - Sync minimap puck colors
- **GetBlueTeamColorFromToaster()** - Read blue color from ToasterReskinLoader
- **GetRedTeamColorFromToaster()** - Read red color from ToasterReskinLoader
- **IsToasterLoaderAvailable** - Check if ToasterReskinLoader is installed

**Key Feature:** All operations use reflection, so ToasterReskinLoader is completely optional. If not installed, methods fail gracefully.

### 2. **ColorIntegrationGuide.cs** (Documentation)
Shows conceptual patterns for integration with checklist.

### 3. **ImplementationGuide.cs** (Concrete Code Examples)
Shows exact code changes to make to your existing files with step-by-step instructions.

---

## How It Works

### Step 1: Initialization (When Scoreboard loads)
```csharp
// In CustomBroadcastScoreboard.Awake()
ToasterReskinLoaderColorSync.SyncTeamColors(
    config.blueTeamColorHex,
    config.redTeamColorHex
);
```

### Step 2: Your Patches Keep Working
Your existing patches (Announcement, Minimap, Leaderboard) continue to apply colors to UI elements exactly as before.

### Step 3: Equipment Colors Get Synced
ToasterReskinLoader automatically receives your colors and applies them to equipment (sticks, helmets, leg pads, tapes).

### Step 4: When Colors Change
```csharp
// When user updates hex colors in config:
ToasterReskinLoaderColorSync.SyncTeamColors(
    newBlueHex,
    newRedHex
);
// Both UI and equipment update automatically
```

---

## Integration Strategies

### **Strategy A: Scoreboard Owns Colors** (Recommended if you want full control)
1. User sets colors in Scoreboard config (hex strings)
2. Scoreboard patches apply colors to UI elements
3. ToasterReskinLoaderColorSync pushes colors to ToasterReskinLoader
4. ToasterReskinLoader applies colors to equipment
5. **Result:** Everything synced, Scoreboard is source of truth

**Pros:**
- Your mod is the config source
- Works whether ToasterReskinLoader is installed or not
- Cleaner UX (single config location)

**Cons:**
- ToasterReskinLoader changes won't affect Scoreboard (one-way sync)

### **Strategy B: Use ToasterReskinLoader's Colors** (If you want ToasterReskinLoader to own the colors)
1. First: Sync your Scoreboard colors to ToasterReskinLoader
2. Then: Your patches READ colors FROM ToasterReskinLoader
3. Your patches apply those colors to UI
4. ToasterReskinLoader applies same colors to equipment
5. **Result:** True bidirectional sync

**Implementation:**
```csharp
// In your patches, use:
Color blueColor = ToasterReskinLoaderColorSync.IsToasterLoaderAvailable
    ? ToasterReskinLoaderColorSync.GetBlueTeamColorFromToaster()
    : ParseHexColor(config.blueTeamColorHex, defaultBlue);
```

**Pros:**
- Single source of truth (ToasterReskinLoader)
- True bidirectional sync

**Cons:**
- Requires ToasterReskinLoader to be installed
- More complex setup

**Recommendation:** Use Strategy A for robustness. It works with or without ToasterReskinLoader.

---

## Implementation Checklist

- [ ] Copy `src/Integration/ToasterReskinLoaderColorSync.cs` to your project
- [ ] Copy `src/Integration/ColorIntegrationGuide.cs` (for reference)
- [ ] Copy `src/Integration/ImplementationGuide.cs` (for reference)
- [ ] In `CustomBroadcastScoreboard.Awake()`: Add color sync call
- [ ] In `CustomBroadcastScoreboard.Helpers.cs`: Add color sync call when colors change
- [ ] Test without ToasterReskinLoader (should work fine)
- [ ] Test with ToasterReskinLoader (should sync both systems)
- [ ] Verify no console errors
- [ ] Verify colors identical in both mods

---

## Benefits

| Benefit | Before | After |
|---------|--------|-------|
| Single color config | ❌ Maintain 2 configs | ✅ One Scoreboard config |
| Equipment colors | ❌ ToasterReskinLoader only | ✅ Scoreboard controls via sync |
| UI colors | ✅ Scoreboard patches | ✅ Scoreboard patches |
| Optional dependencies | ✅ Works without ToasterReskinLoader | ✅ Still works without ToasterReskinLoader |
| Bidirectional sync | ❌ No | ✅ Optional (Strategy B) |
| User experience | ❌ Confusing (2 mods to configure) | ✅ Simple (1 config) |

---

## Code Changes Summary

### Files to Modify
1. **CustomBroadcastScoreboard.cs** - Add sync in Awake()
2. **CustomBroadcastScoreboard.Helpers.cs** - Add sync in color application
3. **Client.Entry.Patches.Announcement.cs** - Optionally use ToasterReskinLoader colors
4. **Client.Entry.Patches.Minimap.cs** - Add minimap sync

### Files to Create
1. **src/Integration/ToasterReskinLoaderColorSync.cs** - Bridge service (already created)
2. **src/Integration/ColorIntegrationGuide.cs** - Docs (already created)
3. **src/Integration/ImplementationGuide.cs** - Code examples (already created)

---

## Troubleshooting

### Colors not syncing to ToasterReskinLoader
- Check console for errors
- Verify `ToasterReskinLoaderColorSync.IsToasterLoaderAvailable` returns true
- Ensure `SyncTeamColors()` is being called

### Colors still not applying to equipment
- ToasterReskinLoader might not be installed (this is OK)
- Equipment colors only apply if ToasterReskinLoader is loaded and enabled
- Check ToasterReskinLoader's settings UI

### Colors applying twice (weird visual effect)
- Ensure you're not calling sync multiple times per frame
- Add a throttle/debounce to color changes

### No errors but nothing happens
- ToasterReskinLoader probably isn't installed
- Your UI colors from Scoreboard should still work
- This is the expected behavior when ToasterReskinLoader isn't present

---

## API Reference

```csharp
// Main methods
ToasterReskinLoaderColorSync.SyncTeamColors(blueHex, redHex);
ToasterReskinLoaderColorSync.SyncMinimapColors(puckHex);

// Query methods
Color blue = ToasterReskinLoaderColorSync.GetBlueTeamColorFromToaster();
Color red = ToasterReskinLoaderColorSync.GetRedTeamColorFromToaster();
bool available = ToasterReskinLoaderColorSync.IsToasterLoaderAvailable;

// All methods fail gracefully if ToasterReskinLoader not installed
// No exceptions thrown - just silent fallback
```

---

## Example Full Integration (One File)

```csharp
// In CustomBroadcastScoreboard.cs

private void Awake()
{
    // ... existing code ...
    
    // Initialize color sync
    if (config != null && ToasterReskinLoaderColorSync.IsToasterLoaderAvailable)
    {
        ToasterReskinLoaderColorSync.SyncTeamColors(
            config.blueTeamColorHex,
            config.redTeamColorHex
        );
        Debug.Log("[Scoreboard] Color sync initialized with ToasterReskinLoader");
    }
}

private void ApplyNativeUIScoreboardTeamColors()
{
    // Your existing color parsing and application code...
    Color blueColor = ParseHexColor(config.blueTeamColorHex, defaultBlue);
    Color redColor = ParseHexColor(config.redTeamColorHex, defaultRed);
    
    // Apply to UI elements (existing code)
    // ...
    
    // NEW: Sync to ToasterReskinLoader
    if (ToasterReskinLoaderColorSync.IsToasterLoaderAvailable)
    {
        ToasterReskinLoaderColorSync.SyncTeamColors(
            config.blueTeamColorHex,
            config.redTeamColorHex
        );
    }
}
```

---

## Next Steps

1. Review `ImplementationGuide.cs` for specific code changes
2. Choose Strategy A or B
3. Make changes to your existing Scoreboard files
4. Build and test
5. Deploy

That's it! Your Scoreboard will now control both UI AND equipment colors.
