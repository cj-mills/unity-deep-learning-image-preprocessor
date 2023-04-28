# Deep Learning Image Preprocessor

Deep Learning Image Preprocessor is a custom Unity package designed for preparing image input to perform inference with deep learning models. The package includes shaders and compute shaders for various image processing tasks, such as cropping, normalizing, and flipping images.

## Features

- Crop image to a specific region
- Normalize image color channels based on mean and standard deviation values
- Flip image along the x-axis
- GPU-accelerated processing



## Demo Projects

| GitHub Repository                                            | Description                                                |
| ------------------------------------------------------------ | ---------------------------------------------------------- |
| [barracuda-image-classification-demo](https://github.com/cj-mills/barracuda-image-classification-demo) | Perform image classification using computer vision models. |
| [barracuda-inference-yolox-demo](https://github.com/cj-mills/barracuda-inference-yolox-demo) | Perform object detection using YOLOX models.               |
| [barracuda-inference-posenet-demo](https://github.com/cj-mills/barracuda-inference-posenet-demo) | Perform 2D human pose estimation using PoseNet models.     |



## Getting Started

### Prerequisites

- Unity game engine

### Installation

You can install the Deep Learning Image Preprocessor package using the Unity Package Manager:

1. Open your Unity project.
2. Go to Window > Package Manager.
3. Click the "+" button in the top left corner, and choose "Add package from git URL..."
4. Enter the GitHub repository URL: `https://github.com/cj-mills/unity-deep-learning-image-preprocessor.git`
5. Click "Add". The package will be added to your project.

For Unity versions older than 2021.1, add the Git URL to the `manifest.json` file in your project's `Packages` folder as a dependency:

```json
{
  "dependencies": {
    "com.cj-mills.deep-learning-image-preprocessor": "https://github.com/cj-mills/unity-deep-learning-image-preprocessor.git",
    // other dependencies...
  }
}

```







## License

This project is licensed under the MIT License. See the [LICENSE](Documentation~/LICENSE) file for details.

