# VicTool

This was a personal tool I was working on to automate price arbitrage opportunities between assets on different exchanges. It is by no means a pretty application, as it was purely a functional tool for myself. In this state it won't run properly for two reasons:
1. It needs Kucoin Exchange authorized API keys which I had hardcoded in (because it was never meant to be released to the public) and removed for pushing to github.
2. The API I was using for some of the DEX price queries has since been deprecated for something newer.

## Description
This tool had the following features:
- A live kucoin orderbook feed for an asset pair
- A live price for a token in an asset pair's liquidity pool on a DEX exchange
- A table which would calculate risk levels and profits for arbitrage opportunities between the two exchanges
- An interface to manually make a spot trade on kucoin
- An interface to manually make a swap on a DEX

It also included a telegram bot which would:
- Ping messages to myself when opportunities under a set of customizable conditions had been reached.
- Send live screenshots of my risk/profit table to my phone.
- Listen for commands to: perform trades with parameters, switch assets, and query prices.

A risky feature I implemented was an automated arbitrage action, where if the appropriate conditions had been met, it would sell or buy on one exchange, immediately transfer the profits to another wallet and perform the opposite action on the other exchange, resulting in a profit while maintaining the original quantity, or better, of an asset.
