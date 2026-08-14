# Glaucoma Simulation System Technical Documentation

## 1. System Overview

此系統旨在 Unity URP (Universal Render Pipeline) 環境下模擬青光眼 (Glaucoma) 患者的視覺體驗。核心模擬效果包含：
*   **視野缺損 (Visual Field Loss / Tunnel Vision)**：周邊視野模糊、變暗且飽和度降低。
*   **動態眼動追蹤 (Gaze Integration)**：清晰視野區域會跟隨使用者的眼球注視點移動。
*   **可程式化視野 (Field Generation)**：透過數據定義視野的清晰與盲區分佈。

## 2. Architecture (系統架構)

系統主要由三個核心元件組成：

1.  **Controller (`GlaucomaRenderer.cs`)**: 負責協調整個系統，即時更新眼球位置數據給 Shader，並串接視野生成器。
2.  **Data Generator (`GlaucomaFieldGenerator.cs`)**: 負責產生一張「敏感度貼圖 (Sensitivity Map)」，定義了視野中哪些角度是清晰的，哪些是模糊的。
3.  **Visual Implementation (`GlaucomaOverlay.shader`)**: 實際執行影像處理的 Shader，根據敏感度貼圖與眼球位置，混合清晰影像與模糊影像。

---

## 3. Component Details (元件詳解)

### 3.1 Controller: `GlaucomaRenderer.cs`
這是掛載於場景中的主要控制腳本。

*   **眼動追蹤整合 (Gaze Tracking)**:
    *   優先嘗試讀取 `LeftGaze` 與 `RightGaze` 的 Transform 位置。
    *   若有真實眼動訊號，將其轉換為螢幕 UV 座標 (`ViewportPoint`)。
    *   **雙眼渲染 (Binocular Support)**: 分別傳送 `_GazeCenterLeft` 與 `_GazeCenterRight` 給 Shader，支援 VR 雙眼獨立清晰區渲染。
*   **模擬回退機制 (Simulation Fallback)**:
    *   若無眼動設備，支援使用 **滑鼠 (Mouse)**、**鍵盤 (WASD)** 或 **自動展示 (Auto Demo)** 模式來模擬注視點移動，方便開發測試。
*   **材質管理**:
    *   在 Runtime 建立 `overlayMaterial` 的實例。
    *   每幀 (Update) 將最新的 `Sensitivity Texture` (來自 Generator) 和 `Gaze Center` 寫入 Shader 參數。

### 3.2 Field Generator: `GlaucomaFieldGenerator.cs`
負責程序化生成一張 RGB 紋理 (Texture2D)，這張圖不直接顯示，而是作為 Shader 的「遮罩 (Mask)」。

*   **運作邏輯**:
    *   **輸入**: 一組 `FieldZone` 列表。每個 Zone 定義了：
        *   `startAngle` - `endAngle`: 視野角度範圍 (例如 0度~10度)。
        *   `sensitivity`: 視覺敏感度 (1.0 = 清晰, 0.0 = 全盲/模糊)。
    *   **生成演算法**:
        *   遍歷 Texture 的每個像素，計算該像素相對於中心點的 UV 距離。
        *   利用 `Mathf.Atan` 將 UV 距離轉換為真實世界的「視角 (Angle in Degrees)」。
        *   根據視角查找對應的 Zone，填入顏色值 (R=G=B=Sensitivity)。
*   **主要功能**:
    *   **Tunnel Vision (隧道視野)**: 預設包含快速生成 20度管狀視野的方法 (`ApplyTunnelVision20`)。
    *   **Gradient Falloff (漸層邊緣)**: 透過多個 Zone 的插值，模擬視野邊緣逐漸模糊變暗的真實感 (`ApplyRealisticTunnel20`)。

### 3.3 Shader Logic: `GlaucomaOverlay.shader`
這是實現視覺效果的核心 Shader，採用 HLSL 編寫。

#### 核心渲染流程 (Fragment Shader):

1.  **計算敏感度 (Determine Mask/Sensitivity)**:
    *   根據目前的像素 UV 與 `_GazeCenter` (注視點) 計算相對偏移量。
    *   利用這個相對偏移量去採樣 `Sensitivity Map` (由 Generator 產生的紋理)。
    *   **結果**: 得到 `sensitivity` 值 (0.0 ~ 1.0)。1.0 代表注視點中心，應保持清晰；0.0 代表周邊，需模糊處理。

2.  **採樣場景 (Sample Scene)**:
    *   `sharpCol`: 直接採樣當前螢幕畫面 (Sharp Scene Color)。

3.  **計算模糊 (Calculate Blur) - Mode 4**:
    *   系統目前主要使用 **Mode 4 (High-Quality Dithered Bokeh)** 進行模糊運算。
    *   **演算法**: 黃金螺旋採樣 (Golden Spiral Sampling)。
    *   在 Shader 迴圈中進行約 32 次採樣 (Samples)。
    *   每次採樣依據黃金角度 (Golden Angle) 旋轉並擴散半徑，模擬散景 (Bokeh) 效果。
    *   此方法比單純的高斯模糊 (Gaussian) 更具美感，且能模擬視力受損的光暈感。

4.  **周邊視覺特效 (Apply Defects)**:
    *   對模糊後的影像 (`blurCol`) 施加額外特效以模擬視神經受損：
        *   **Desaturation**: 降低飽和度 (轉為黑白)。
        *   **Darkness**: 降低亮度 (周邊變暗)。

5.  **最終合成 (Final Composite)**:
    *   使用 `sensitivity` 作為權重，在 `sharpCol` (清晰) 與 `blurCol` (模糊/變暗) 之間做線性插值 (Lerp)。
    *   `FinalColor = lerp(BlurWithEffects, SharpOriginal, Sensitivity)`。

---

## 4. Mathematical Model of Gaze Projection & Visual Field (數學模型)

本節以學術角度說明 3D 空間中的眼動訊號 (Gaze Point) 如何被投影至 2D 平面 (RawImage/Screen)，以及視野缺損圖 (Sensitivity Map) 如何對應到 110° FOV 的頭盔視野。

### 4.1 3D Gaze to 2D Plane Projection (3D-to-2D 投影)

系統使用標準的 **透視投影 (Perspective Projection)** 將 3D 世界座標轉換為 2D 視埠座標 (Viewport Coordinates)。此過程模擬了人眼或攝影機將三維物體成像於視網膜或感光元件的過程。

假設：
*   $P_{world} = [x, y, z, 1]^T$ 為 Gaze Tracker 在世界座標系中的位置向量。
*   $M_{view}$ 為攝影機的觀察矩陣 (View Matrix)，負責將座標轉至攝影機空間。
*   $M_{proj}$ 為攝影機的投影矩陣 (Projection Matrix)。

計算步驟如下：

1.  **Clip Space Transformation (裁切空間轉換)**:
    $$P_{clip} = M_{proj} \cdot M_{view} \cdot P_{world}$$
    此時 $P_{clip} = [x_c, y_c, z_c, w_c]^T$。

2.  **Perspective Division (透視除法)**:
    將 Clip Space 座標標準化為 NDC (Normalized Device Coordinates)：
    $$P_{ndc} = \frac{P_{clip}}{w_c} = [x_n, y_n, z_n, 1]^T$$
    其中 $x_n, y_n \in [-1, 1]$。

3.  **Viewport Transformation (視埠轉換)**:
    將 NDC 映射到 $[0, 1]$ 的 UV 空間 (對應 Unity 的 `ViewportPoint`)：
    $$u_{gaze} = 0.5 \cdot x_n + 0.5$$
    $$v_{gaze} = 0.5 \cdot y_n + 0.5$$

此 $(u_{gaze}, v_{gaze})$ 即為 Shader 中的 `_GazeCenter`，代表視野清晰區在螢幕畫面上的中心點。

### 4.2 Visual Field Mapping & 110° FOV (視野映射與 110度 FOV)

在 `GlaucomaFieldGenerator` 中，我們產生一張代表視野敏感度的紋理。為了正確模擬 VR 頭盔 (如 110° FOV)，我們使用 **Rectilinear Projection (直線投影/針孔相機模型)** 來定義紋理上每一點對應的真實視角 $\theta$。

假設：
*   $FOV_{ref} = 110^\circ$ (參考視場角，對應頭盔規格)。
*   $(u, v)$ 為紋理上的歸一化座標，範圍 $[-1, 1]$ (以紋理中心為原點)。
*   $r_{uv} = \sqrt{u^2 + v^2}$ 為像素點距離紋理中心的徑向距離。

數學關係式如下：

$$r_{uv} = \frac{\tan(\theta)}{\tan(\frac{FOV_{ref}}{2})}$$

反之，對於紋理上的任一點，其實際視角 $\theta$ 計算公式為：

$$\theta = \arctan \left( r_{uv} \cdot \tan\left(\frac{FOV_{ref}}{2} \cdot \frac{\pi}{180}\right) \right)$$

**物理意義說明**:
*   當 $r_{uv} = 0$ (中心點) 時，$\theta = 0^\circ$ (中心視野)。
*   當 $r_{uv} = 1$ (紋理邊緣) 時，$\theta = \frac{FOV_{ref}}{2} = 55^\circ$ (即總視場角 110° 的邊緣)。

透過此公式，我們能確保在 Shader 中採樣到的 Sensitivity 值，精確對應到臨床定義的視野角度 (例如 10° 隧道視野)，無論使用者的螢幕或頭盔 FOV 大小為何，只要 $FOV_{ref}$ 設定正確，投影出的盲區範圍即符合光學定義。

專案中包含一組尚未完全啟用或處於除錯狀態的 URP Render Feature 元件：
*   **`GlaucomaBlurFeature.cs`** & **`GlaucomaBlur.shader`**:
    *   **目的**: 試圖利用 Render Feature 在渲染管線早期預先產生一張降解析度的全屏模糊貼圖，以節省 Shader 內的即時採樣效能。
    *   **現狀**: 代碼中大部分邏輯被註解或處於 Debug 狀態 (例如輸出 `Color.green` 或紅色)，顯示目前生產環境主要依賴 `GlaucomaOverlay.shader` 內部的 Mode 4 即時模糊運算，而非此 Render Feature。

## 5. Summary Flowchart

```mermaid
graph TD
    A[Update (Every Frame)] -->|Get Eye Position| B(GlaucomaRenderer)
    C[GlaucomaFieldGenerator] -->|Generate Sensitivity Map| D{Texture Asset}
    D --> B
    B -->|Set Material Properties| E[GlaucomaOverlay.shader]
    
    subgraph Shader Operation
    E --> F{Calculate UV Offset from Gaze}
    F -->|Sample| D
    D -->|Return Sensitivity 0~1| G[Mixer]
    H[Scene Color Sharp] --> G
    I[Scene Color Blurred] -->|Desaturate & Darken| G
    G -->|Lerp based on Sens| J[Final Pixel Color]
    end
```
