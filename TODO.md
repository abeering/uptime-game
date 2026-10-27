# TODO — WAVING / Natural LOAD Noise

WAVING V0 works well, but the one-decimal LOAD readout can feel metronomic because its changes occur at predictable intervals.

When introducing WAVING to generated connections, evaluate it alongside existing natural LOAD noise before changing anything.

If necessary, experiment with faster, irregular noise cadence (~1 second) and a small absolute jitter floor (~±0.1 LOAD), preserving the recognizable underlying waveform.

Goal: make individual LOAD changes feel organic without sacrificing the larger behavioral pattern.
