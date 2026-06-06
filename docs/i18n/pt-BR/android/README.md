# Aether Media — Android

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](../../es/android/README.md) · [العربية](../../ar/android/README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](../../ja/android/README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](README.md) · [Русский](../../ru/android/README.md) · [فارسی](../../fa/android/README.md) · [한국어](../../ko/android/README.md)

Dois aplicativos Android desenvolvidos com Jetpack Compose e media3/ExoPlayer, oferecendo a experiência completa do Aether Media em celulares e Android TV — incluindo descoberta offline via mesh, transmissão ao vivo, sessões de assistir juntos e interações sociais — sem necessidade de conexão com a internet.

---

## Aplicações

| Módulo | Pacote | Destino |
|--------|---------|--------|
| `media/` | `aethernet.media` | Celular / tablet (Jetpack Compose) |
| `media-tv/` | `aethernet.media.tv` | Android TV (lean-back, navegação por D-pad) |

---

## Requisitos

- Android Studio Hedgehog (2023.1) ou superior
- Android SDK: `compileSdk 35`, `minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## Build

```bash
# Aplicativo para celular
cd media
./gradlew assembleDebug

# Aplicativo para TV
cd media-tv
./gradlew assembleDebug
```

### Build de release

```bash
./gradlew assembleRelease
```

Defina as credenciais de assinatura em `local.properties` ou por meio de variáveis de ambiente antes de gerar um APK de release.

---

## Executar testes

```bash
./gradlew test          # testes unitários
./gradlew connectedTest # testes instrumentados (dispositivo / emulador necessário)
```

---

## Arquitetura

Ambos os aplicativos seguem o mesmo padrão MVVM baseado nos componentes Jetpack:

```
UI Layer       — Telas Compose + ViewModels
Domain Layer   — Casos de uso (compartilhados com o módulo kotlin/ JVM)
Data Layer     — Transporte via mesh Aether usando os bindings Android do aether-protocol
```

### Telas principais (aplicativo para celular)

| Tela | Descrição |
|--------|-------------|
| Home | Feed de conteúdo dos criadores seguidos |
| Nearby | Transmissões ao vivo descobertas via mesh (sem internet) |
| Library | Mídia local e baixada |
| Watch Together | Sessões ativas de assistir juntos |
| Profile | Identidade AetherNetTag e canal do criador |

### Telas principais (aplicativo para TV)

| Tela | Descrição |
|--------|-------------|
| Browse | Navegador de conteúdo no estilo Leanback |
| Playback | ExoPlayer em tela cheia com controles por D-pad |
| Nearby | Descoberta de peers via mesh exibida como uma fila de cards |

---

## Motor de mídia

Ambos os aplicativos utilizam **media3/ExoPlayer** para reprodução:

- Streaming adaptativo HLS e DASH a partir da mesh Aether local
- Suporte a faixas de legenda (SRT, VTT)
- Reprodução em segundo plano via `MediaSessionService`
- Picture-in-picture (PiP) no Android 8.0+

---

## Integração com a mesh

Os aplicativos se conectam ao serviço Android do Protocolo Aether na inicialização:

```kotlin
// Resolver peers próximos com capacidade de streaming
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

Ordem de negociação de transporte: **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

Os fragmentos de conteúdo são distribuídos via `IContentService`; as transmissões ao vivo utilizam `IStreamingService`. Tudo funciona peer-to-peer sem servidor central.

---

## Sessões de assistir juntos

```kotlin
// Hospedar uma sessão de assistir juntos
val session = watchTogether.hostAsync(contentHash)

// Participantes entram pelo AetherNetTag
watchTogether.joinAsync(hostUhid)
```

A reprodução é sincronizada com tolerância de ±100 ms (compensação de RTT). Reações com emoji são sobrepostas ao vídeo em tempo real.

---

## Dependências

| Biblioteca | Finalidade |
|---------|---------|
| `media3-exoplayer` | Reprodução de vídeo/áudio |
| `media3-session` | Sessão de mídia + reprodução em segundo plano |
| `androidx.compose.ui` | Kit de ferramentas de UI |
| `androidx.leanback` | Navegação para TV (somente media-tv) |
| `aether-protocol-android` | Transporte via mesh |

---

## Estrutura do projeto

```
android/
├── media/                  # Aplicativo para celular / tablet
│   ├── app/
│   │   └── src/main/
│   │       ├── kotlin/     # ViewModels, telas, UI Compose
│   │       └── res/        # Layouts, drawables, strings
│   └── build.gradle.kts
└── media-tv/               # Aplicativo para Android TV
    ├── app/
    │   └── src/main/
    │       ├── kotlin/     # Fragments Leanback, presenters
    │       └── res/
    └── build.gradle.kts
```

---

## Licença

MIT
