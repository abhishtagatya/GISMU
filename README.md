# 🛰  GISMU: Geospatial Visualization in Unity

![Project Preview](Docs/preview.png)  
*A Unity-based 3D visualization of geospatial data in Brno, CZ.*

---

## Objective

- 🔍 **Explore and Visualize Geospatial Data** and how it can be visualized effectively in Unity
- 🗺️ **Interact with Terrain Data**, allowing users to interact and experiment with spatial information
- 🥽 **(Potential) VR Support** for general-purpose geospatial data projects and immersive exploration

## Features

- 📁 **Shapefile Support**: Parses and displays both Polygon and LineString geometries
- 🗻 **Terrain Reconstruction**: Uses elevation data from Brno’s 2019 digital terrain model (DTM)
- ⚙️ **Modular Architecture**: Designed for extension to new datasets or interaction methods

## Datasets Used

1. **[Digital Terrain Model of Brno (2019)](https://data.brno.cz/datasets/mestobrno::digit%C3%A1ln%C3%AD-model-ter%C3%A9nu-brna-2019-brno-digital-terrain-model-2019/about)**  
   Used to generate terrain mesh and elevation data.

2. **[Public Transport Intensity - Bus](https://data.brno.cz/datasets/mestobrno::intenzity-mhd-autobus-public-transport-intensity-bus/about)**  
   Used to visualize bus movement intensity over Brno.

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/brno-geodata-visualizer.git
   ```

2. Download the Dataset (used for Demo) to `Data` folder.
3. Start the Project in Unity (URP)
