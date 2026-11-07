# Owl Publishing Kit

Owl is a Unity toolkit for coordinating common mobile game publishing services from one settings window and startup flow.

## Features

- Startup coordination for internet checks, in-app purchases, consent, analytics, and ads.
- Google Mobile Ads and AppLovin MAX placement support.
- In-app purchase setup, remove-ads entitlement, and restore support.
- Firebase Analytics, Crashlytics, Remote Config, and Cloud Messaging integrations.
- GameAnalytics, Meta events, Airbridge, and SolarEngine integrations.
- Runtime controls for ad placement initialization, remote ad flags, and diagnostic status.

Optional provider SDKs are not bundled. Install them from Unity's **Owl > SDKs** menu and configure each integration in **Owl > Settings**. Add the provider SDKs and credentials required by the services you enable.

## Install in Unity

1. Open **Window > Package Management > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter `https://github.com/sadiqhusaini/OwlPublishingKit.git`.
4. Open **Owl > Settings**, choose your providers, configure ad units and products, then save.
5. Use **Owl > Create Owl** to add the Owl manager to the startup scene.

The package declares Unity UI and In-App Purchasing dependencies. Provider SDKs such as Firebase, Google Mobile Ads, AppLovin MAX, GameAnalytics, Meta, Airbridge, and SolarEngine remain optional and must be installed/configured separately.

## Manual ad initialization

Interstitial, rewarded interstitial, and rewarded placements can be initialized when the game needs them. Call these after Owl startup and the ad provider are ready:

```csharp
using Owl.Monetization;

AdManager.InitializeInterstitial();
AdManager.InitializeRewardedInterstitial();
AdManager.InitializeRewarded();
```

The corresponding **On Start** settings enable startup initialization. Placement methods respect Owl settings, consent, remove-ads ownership, remote controls, and provider support.

## Startup and diagnostics

Add the Owl manager to an early startup scene and enable **Automatic startup** in Owl Settings. The optional Owl status canvas reports service readiness, initialization, ad availability, and recent flow events.

## Notes

- This repository contains Owl's Unity assets and source, plus a blank ad configuration asset. Set your own provider credentials and ad unit IDs in your project.
- The optional third-party Unity Logs Viewer has been omitted from this public package.
- No license file is included yet; public visibility does not grant permission to redistribute or modify the code.
