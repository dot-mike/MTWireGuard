<a id="readme-top"></a>


<!-- PROJECT LOGO -->
<br />
<div align="center">

[![GitHub Stars](https://img.shields.io/github/stars/dot-mike/MTWireGuard.svg)](https://github.com/dot-mike/MTWireGuard/stargazers)
[![GitHub Issues](https://img.shields.io/github/issues/dot-mike/MTWireGuard.svg)](https://github.com/dot-mike/MTWireGuard/issues)
[![Current Version](https://img.shields.io/docker/v/techgarageir/mtwireguard)](https://github.com/dot-mike/MTWireGuard)
  
  <a href="https://github.com/dot-mike/MTWireGuard">
    <img src="/Photos/logo.png" alt="Logo" width="400">
  </a>

  <h3 align="center">MTWireguard</h3>

  <p align="center">
    An awesome way to manage Mikrotik Wireguard interface!
    <br />
    <a href="https://github.com/dot-mike/MTWireGuard/issues/new?labels=bug">Report Bug</a>
    ·
    <a href="https://github.com/dot-mike/MTWireGuard/issues/new?labels=enhancement">Request Feature</a>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>

<!-- ABOUT THE PROJECT -->
## About The Project

[![MTWireguard screenshot][product-screenshot]](https://github.com/dot-mike/MTWireGuard)

This is the first and most **Stable** and **Secure** Accounting system built to manage Wireguard interface on Mikrotik (CHR and devices). You can easily make a Virtual Private Network based on Wireguard protocol in your RouterOS device.

Here's why:
* Ability to install on RouterOS itself (using Docker)
* Compatible with future RouterOS updates
* Storing all your data on your own server

> **Disclaimer:** This project is only for education. Do not use it for illegal purposes.<br>Any illegal usage of this software is solely the responsibility of the user. The creators and contributors of this project aren't liable for any misuse or damages caused by improper usage of this software.

<p align="right">(<a href="#readme-top">back to top</a>)</p>


### Built With

This project is built using the following frameworks/libraries.

* [![DotNet][.Net]][.Net-url]
* [![GitHub Actions][GitHub-Actions]][GitHub-Actions-url]
* [![SQLite][SQLite]][SQLite-url]
* [![Docker][Docker]][Docker-url]
* [![SwaggerUI][Swagger]][Swagger-url]
* [![Bootstrap][Bootstrap.com]][Bootstrap-url]
* [![JQuery][JQuery.com]][JQuery-url]

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- GETTING STARTED -->
## Getting Started

This is an example of how you may give instructions on setting up your project locally.
To get a local copy up and running follow these simple example steps.

### Prerequisites

Requirements to install MTWireguard on a RouterOS system (recommended).
* Mikrotik device or CHR running ![RouterOS]
* A minimum of 2GB Ram
* A minimum of 5GB Disk

### Installation

_As of this project is based on docker, you can install on any OS that can run docker containers such as Linux, Windows or RouterOS itself._

#### Method 1 (recommended)
<a href="https://t.me/MTWireguard/8">Install on RouterOS</a>

#### Method 2
Installing on VPS using docker

#### Method 3
**Reverse Proxy Setup with TLS (Caddy/nginx/Traefik)**

MTWireGuard supports running behind reverse proxies with TLS termination. This is recommended for production deployments.

See the [Reverse Proxy Setup Guide](docs/reverse-proxy-setup.md) for detailed configuration examples with:
- Caddy
- nginx  
- Traefik

Quick start with reverse proxy:
```bash
# Use the reverse proxy docker-compose template
cp docker-compose.reverse-proxy.yml docker-compose.yml
# Edit the configuration for your environment
docker-compose up -d
```

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- USAGE EXAMPLES -->
## Usage

_For examples, please refer to the [Documentation](https://github.com/dot-mike/MTWireGuard/wiki)_

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ROADMAP -->
## Roadmap

- [x] Add traffic-usage limit
- [x] Add expiration date
- [x] Add auto addressing (via /ip/pool)
- [x] Add auto build via github-actions
- [ ] Add "comments" on peers
- [ ] Add backup-restore configuration
- [ ] Add bandwidth limit

See the [open issues](https://github.com/dot-mike/MTWireGuard/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Top contributors:

<a href="https://github.com/dot-mike/MTWireGuard/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=dot-mike/MTWireGuard" alt="contrib.rocks image" />
</a>

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[product-screenshot]: /Photos/screenshot.png
[Mikrotik]: https://img.shields.io/badge/mikrotik-%23676867.svg?style=for-the-badge&logo=mikrotik&logoColor=white
[Wireguard]: https://img.shields.io/badge/wireguard-%2388171A.svg?style=for-the-badge&logo=wireguard&logoColor=white
[Telegram-Channel]: https://img.shields.io/badge/Telegram-Channel-2CA5E0?style=for-the-badge&logo=telegram&logoColor=white
[Telegram-Group]: https://img.shields.io/badge/Telegram-Group-2CA5E0?style=for-the-badge&logo=telegram&logoColor=white
[.Net]: https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white
[.Net-url]: https://dotnet.microsoft.com/
[GitHub-Actions]: https://img.shields.io/badge/github%20actions-%232671E5.svg?style=for-the-badge&logo=githubactions&logoColor=white
[GitHub-Actions-url]: https://github.com/dot-mike/MTWireGuard/actions
[SQLite]: https://img.shields.io/badge/sqlite-%2307405e.svg?style=for-the-badge&logo=sqlite&logoColor=white
[SQLite-url]: https://reactjs.org/
[Docker]: https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white
[Docker-url]: https://docker.com/
[Swagger]: https://img.shields.io/badge/-Swagger-%23Clojure?style=for-the-badge&logo=swagger&logoColor=white
[Swagger-url]: https://angular.io/
[Bootstrap.com]: https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white
[Bootstrap-url]: https://getbootstrap.com
[JQuery.com]: https://img.shields.io/badge/jQuery-0769AD?style=for-the-badge&logo=jquery&logoColor=white
[JQuery-url]: https://jquery.com 
[RouterOS]: https://img.shields.io/badge/RouterOS-7.15+-blue?logo=mikrotik
