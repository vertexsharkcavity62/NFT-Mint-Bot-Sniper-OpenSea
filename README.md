# NFT Mint Bot Sniper | Auto-Detect + Mint + List on OpenSea/Blur | C#

![Build](https://img.shields.io/github/actions/workflow/status/your-org/NFT-Mint-Bot-Sniper-OpenSea/build.yml?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)
![Stars](https://img.shields.io/github/stars/your-org/NFT-Mint-Bot-Sniper-OpenSea?style=flat-square)

Automated NFT mint sniper that detects new contract deployments on Ethereum, analyzes bytecode for mint functions, executes mints with gas optimization, and auto-lists on OpenSea and Blur.

---

## Features

- **Real-time contract detection** — monitors pending transactions for new ERC-721/ERC-1155 deployments
- **Bytecode analysis** — decodes deployed contracts, detects mint/claim/purchase function selectors
- **Gas boosting** — dynamically adjusts gas price with configurable multipliers and urgency levels
- **Multi-wallet rotation** — distributes mints across a pool of wallets with cooldowns
- **Whitelist checking** — verifies merkle proofs and on-chain whitelist status
- **Rarity prediction** — estimates collection rarity from metadata trait distributions
- **Auto-listing** — instantly lists minted NFTs on OpenSea and Blur at calculated prices
- **Smart filtering** — filters by collection name, mint price, creator reputation score
- **Notifications** — real-time alerts via Discord webhooks and Telegram bot API
- **IPFS resolution** — resolves ipfs:// and ar:// URIs through multiple gateways

## Architecture

```
Program.cs (Host + DI)
  └── SniperEngine (BackgroundService)
        ├── ContractDeployListener → polls new deploys
        ├── ContractAnalyzer → bytecode analysis
        │     ├── ContractDecoder → token standard detection
        │     └── MintFunctionDetector → selector matching
        ├── Filters (Collection / Price / Creator)
        ├── QueueManager → priority queue
        ├── MintExecutor
        │     ├── MintCaller → tx construction
        │     ├── GasBooster → gas optimization
        │     └── WalletRotator → wallet pool
        ├── AutoLister
        │     ├── OpenSeaClient
        │     └── BlurClient
        └── Notifications
              ├── DiscordWebhook
              └── TelegramAlert
```

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Ethereum RPC endpoint (Alchemy, Infura, or self-hosted)
- API keys for OpenSea and/or Blur (optional)

## Build

```bash
dotnet restore src/NftSniper/NftSniper.csproj
dotnet build src/NftSniper/NftSniper.csproj -c Release
```

## Usage

```bash
# Set required environment variables
export ETH_RPC_URL="https://eth-mainnet.g.alchemy.com/v2/YOUR_KEY"
export WALLET_KEYS="privatekey1:main,privatekey2:alt"

# Optional
export OPENSEA_API_KEY="your-opensea-key"
export BLUR_API_KEY="your-blur-key"
export DISCORD_WEBHOOK_URL="https://discord.com/api/webhooks/..."
export TELEGRAM_BOT_TOKEN="123456:ABC-DEF"
export TELEGRAM_CHAT_ID="-1001234567890"

# Run
dotnet run --project src/NftSniper/NftSniper.csproj
```

## Configuration

All configuration is loaded from environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `ETH_RPC_URL` | Ethereum JSON-RPC endpoint | `https://eth-mainnet.g.alchemy.com/v2/demo` |
| `ETH_WSS_URL` | WebSocket RPC endpoint | `wss://eth-mainnet.g.alchemy.com/v2/demo` |
| `WALLET_KEYS` | Comma-separated `privatekey:label` pairs | — |
| `MAX_MINT_PRICE` | Maximum mint price in ETH | `0.1` |
| `MAX_GAS_GWEI` | Gas price cap in Gwei | `100` |
| `OPENSEA_API_KEY` | OpenSea API key | — |
| `BLUR_API_KEY` | Blur API key | — |
| `DISCORD_WEBHOOK_URL` | Discord webhook for alerts | — |
| `TELEGRAM_BOT_TOKEN` | Telegram bot token | — |
| `TELEGRAM_CHAT_ID` | Telegram chat ID for alerts | — |

## Disclaimer

This project is provided for **educational and research purposes only**. Use at your own risk. The authors are not responsible for any financial losses or violations of third-party terms of service. Always ensure you have proper authorization before interacting with any blockchain network or marketplace API.

## License

MIT
