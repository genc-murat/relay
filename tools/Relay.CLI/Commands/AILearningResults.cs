namespace Relay.CLI.Commands;

public class AILearningResults
{
    public long TrainingSamples { get; set; }
    public double ModelAccuracy { get; set; }
    public double TrainingTime { get; set; }
    public ImprovementArea[] ImprovementAreas { get; set; } = Array.Empty<ImprovementArea>();
}
