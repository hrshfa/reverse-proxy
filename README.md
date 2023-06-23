# reverse-proxy
a asp.net core 6 web api for reverse proxy
for use this:
1-> add addresses and aliases to appsettings.json like code below:
    "HostsUrls": {
    "google": "https://www.google.com",
    "googlestatic": "https://www.gstatic.com",
    "googleforms": "https://docs.google.com/forms",

    "kucoin": "https://api.kucoin.com",
    "testkucoin": "https://openapi-sandbox.kucoin.com",
    "binance": "https://api.binance.com",
    "binance1": "https://api1.binance.com",
    "binance2": "https://api2.binance.com",
    "binance3": "https://api3.binance.com",
    "binance4": "https://api4.binance.com",
    "testbinance": "https://testnet.binance.vision",
    "gateio": "https://api.gateio.ws/api/v4",
    "gateiofx": "https://fx-api.gateio.ws/api/v4",
    "bybit": "https://api.bybit.com",
    "bybit1": "https://api.bytick.com ",
    "coinex": "https://api.coinex.com/v1"
  }
