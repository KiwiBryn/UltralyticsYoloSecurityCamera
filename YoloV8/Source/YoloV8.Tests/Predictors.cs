﻿namespace YoloV8.Tests;

public static class Predictors
{
    public static readonly YoloV8Predictor Pose = YoloV8Predictor.Create("./models/yolov8n-pose-uint8.onnx");
    public static readonly YoloV8Predictor Detection = YoloV8Predictor.Create("./models/yolov8n-uint8.onnx");
    public static readonly YoloV8Predictor Segmentation = YoloV8Predictor.Create("./models/yolov8n-seg-uint8.onnx");
    public static readonly YoloV8Predictor Classification = YoloV8Predictor.Create("./models/yolov8n-cls-uint8.onnx");

    public static YoloV8Predictor GetPredictor(YoloV8Task task)
    {
        return task switch
        {
            YoloV8Task.Pose => Pose,
            YoloV8Task.Detect => Detection,
            YoloV8Task.Segment => Segmentation,
            YoloV8Task.Classify => Classification,
            _ => throw new InvalidEnumArgumentException()
        };
    }
}