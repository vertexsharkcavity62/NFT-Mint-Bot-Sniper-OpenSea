# Changelog

## [v1.0.0] - 2026-07-19

### Added
- Auto-listing on OpenSea and Blur with dynamic floor price calculation
- Creator reputation scoring based on deployment history
- Rarity prediction from metadata trait distributions
- Discord and Telegram notification channels
- Multi-wallet rotation with cooldown management

### Changed
- Upgraded to .NET 9 with modern C# patterns
- Switched to priority queue for mint target ordering

### Fixed
- Gas estimation accuracy for ERC-1155 batch mints
- IPFS gateway fallback when primary gateway times out

---

## [v0.9.0] - 2026-06-15

### Added
- Whitelist checker with merkle proof verification
- Collection name and price filters
- IPFS and Arweave URI resolution with multi-gateway support
- NFT metadata parser with trait extraction

### Changed
- Refactored contract analysis into separate decoder and detector components
- Improved gas boosting algorithm with urgency levels

---

## [v0.8.0] - 2026-05-20

### Added
- Core sniper engine as BackgroundService
- Contract deployment listener via pending transaction polling
- Bytecode analysis for ERC-721 and ERC-1155 detection
- Mint function selector matching (mint, claim, purchase, freeMint)
- Gas booster with configurable multiplier and cap
- Basic wallet pool with environment variable configuration
- Queue manager with priority scoring
