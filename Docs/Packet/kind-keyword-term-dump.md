public enum PacketKind
{
    None,

    // 🔴 Threats (classic / grounded)
    Trojan,
    Rootkit,
    Keylogger,
    Ransomware,
    Backdoor,
    Botnet,
    Cryptominer,
    ExploitKit,
    Dropper,
    Wiper,

    // 🔴 Threats (network / expressive)
    PacketStorm,
    FloodBurst,
    SlowLoris,
    Amplifier,
    ReflectionAttack,
    PortScanner,
    LateralMover,
    Beacon,
    Sniffer,
    SessionHijack,

    // 🔴 Threats (flavor / weird)
    Polymorph,
    GhostProcess,
    PhantomPing,
    EchoInjector,
    SignalLeech,
    DriftCode,
    NullPayload,
    TimeBomb,
    Cascade,
    Shard,

    // 🔵 Priority (infra / real-ish)
    DNSQuery,
    AuthRequest,
    Handshake,
    Heartbeat,
    Sync,
    Replication,
    Backup,
    Failover,
    Certificate,
    TokenRefresh,

    // 🔵 Priority (business / system)
    Payment,
    Checkout,
    OrderSubmit,
    InventoryUpdate,
    ShipmentNotice,
    Alert,
    Telemetry,
    Metrics,
    AuditLog,
    ConfigPush,

    // 🔵 Priority (high-stakes / gameplay)
    Override,
    RootAccess,
    KillSwitch,
    Migration,
    Recovery,
    Hotfix,
    PatchDeploy,
    Escalation,
    Broadcast,
    Command,

    // 🟢 Benign (normal traffic)
    HttpRequest,
    AssetLoad,
    ImageFetch,
    VideoStream,
    ChatMessage,
    Email,
    SearchQuery,
    ApiCall,
    CacheMiss,
    Redirect,

    // 🟢 Benign (background)
    Ping,
    KeepAlive,
    Poll,
    SyncCheck,
    HeartbeatLite,
    Presence,
    TypingIndicator,
    AdRequest,
    Tracker,
    Prefetch,

    // 🟢 Benign (flavor)
    Meme,
    CatVideo,
    SpamMail,
    Newsletter,
    SocialPing,
    FriendRequest,
    LikeEvent,
    CommentPost,
    StatusUpdate,
    StoryUpload
}