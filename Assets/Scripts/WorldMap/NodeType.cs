public enum NodeType
{
    // Legacy values preserved for existing assets/inspector links.
    Battle = 0,
    Chest = 1,
    Rest = 2,
    EliteBattle = 3,
    BossBattle = 4,
    Event = 5,
    Merchant = 6,

    // New concept aliases and extensions.
    NormalBattle = Battle,
    Elite = EliteBattle,
    Shop = Merchant,
    RandomEvent = Event,
    Boss = BossBattle,
    AlchemyPot = 7,
    TrialGate = 8,
    GamblingBazaar = 9,
    FortuneWheel = 10,
    Crossroads = 11,
    CardWorkshop = 12,
    StorageVault = 13,
    Chronicler = 14,
    SeerEye = 15,
    MemoryCandle = 16,
    MysticalAltar = 17
}
