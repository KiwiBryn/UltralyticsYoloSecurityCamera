﻿namespace Compunet.YoloV8;

public static partial class YoloV8Extensions
{
    public static PoseResult Pose(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        configuration ??= predictor.Configuration;

        predictor.ValidatePoseShape();

        return predictor.Run(selector, (outputs, image, timer) =>
        {
            var output = outputs[0].AsTensor<float>();

            var parser = new PoseOutputParser(predictor.Metadata, configuration);

            var boxes = parser.Parse(output, image);

            var speed = timer.Stop();

            return new PoseResult
            {
                Boxes = boxes,
                Image = image,
                Speed = speed,
            };
        }, configuration);
    }

    public static DetectionResult Detect(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        configuration ??= predictor.Configuration;

        predictor.ValidateTask(YoloV8Task.Detect);

        return predictor.Run(selector, (outputs, image, timer) =>
        {
            var output = outputs[0].AsTensor<float>();

            var parser = new DetectionOutputParser(predictor.Metadata, configuration);

            var boxes = parser.Parse(output, image);

            var speed = timer.Stop();

            return new DetectionResult
            {
                Boxes = boxes,
                Image = image,
                Speed = speed,
            };
        }, configuration);
    }

    public static ObbDetectionResult DetectObb(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        configuration ??= predictor.Configuration;

        predictor.ValidateTask(YoloV8Task.Obb);

        return predictor.Run(selector, (outputs, image, timer) =>
        {
            var output = outputs[0].AsTensor<float>();

            var parser = new ObbDetectionOutputParser(predictor.Metadata, configuration);

            var boxes = parser.Parse(output, image);

            var speed = timer.Stop();

            return new ObbDetectionResult
            {
                Boxes = boxes,
                Image = image,
                Speed = speed,
            };
        }, configuration);
    }

    public static SegmentationResult Segment(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        configuration ??= predictor.Configuration;

        predictor.ValidateTask(YoloV8Task.Segment);

        return predictor.Run(selector, (outputs, image, timer) =>
        {
            var parser = new SegmentationOutputParser(predictor.Metadata, configuration);

            var boxesOutput = outputs[0].AsTensor<float>();
            var maskPrototypes = outputs[1].AsTensor<float>();

            var boxes = parser.Parse(boxesOutput, maskPrototypes, image);

            var speed = timer.Stop();

            return new SegmentationResult
            {
                Boxes = boxes,
                Image = image,
                Speed = speed,
            };
        }, configuration);
    }

    public static ClassificationResult Classify(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        configuration ??= predictor.Configuration;

        predictor.ValidateTask(YoloV8Task.Classify);

        return predictor.Run(selector, (outputs, image, timer) =>
        {
            var output = outputs[0].AsEnumerable<float>().ToList();

            var probs = new ClassProbability[output.Count];

            for (int i = 0; i < output.Count; i++)
            {
                var name = predictor.Metadata.Names[i];
                var confidence = output[i];

                probs[i] = new ClassProbability
                {
                    Name = name,
                    Confidence = confidence,
                };
            }

            var top = probs.MaxBy(x => x.Confidence) ?? throw new Exception();

            var speed = timer.Stop();

            return new ClassificationResult
            {
                TopClass = top,
                Probabilities = probs,
                Image = image,
                Speed = speed,
            };
        }, configuration);
    }

    #region Async Operations

    public static async Task<PoseResult> PoseAsync(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        return await Task.Run(() => predictor.Pose(selector, configuration));
    }

    public static async Task<DetectionResult> DetectAsync(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        return await Task.Run(() => predictor.Detect(selector, configuration));
    }

    public static async Task<ObbDetectionResult> DetectObbAsync(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        return await Task.Run(() => predictor.DetectObb(selector, configuration));
    }

    public static async Task<SegmentationResult> SegmentAsync(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        return await Task.Run(() => predictor.Segment(selector, configuration));
    }

    public static async Task<ClassificationResult> ClassifyAsync(this YoloV8Predictor predictor, ImageSelector selector, YoloV8Configuration? configuration = null)
    {
        return await Task.Run(() => predictor.Classify(selector, configuration));
    }

    #endregion

    private static void ValidateTask(this YoloV8Predictor predictor, YoloV8Task task)
    {
        if (predictor.Metadata.Task != task)
        {
            throw new InvalidOperationException("The loaded model does not support this task");
        }
    }

    private static void ValidatePoseShape(this YoloV8Predictor predictor)
    {
        predictor.ValidateTask(YoloV8Task.Pose);

        if (predictor.Metadata is YoloV8PoseMetadata metadata)
        {
            var shape = metadata.KeypointShape;

            if (shape.Channels is 2 or 3)
            {
                return;
            }
        }

        throw new NotSupportedException("The this keypoint shape is not supported");
    }
}