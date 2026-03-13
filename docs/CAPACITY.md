# Capacity Guidance (Single Node)

## Baseline tuning knobs

- `--max-parallelism`: controls concurrent ready-step execution.
- `--default-timeout-ms`: prevents hung steps from stalling the node.
- `--default-retries`: balances resiliency vs runtime cost.

## Starting point

- CPU-bound workflows: set max parallelism near logical core count.
- IO-bound workflows: start with `2x` logical core count, then measure.
- Use conservative retries (`1-2`) with exponential backoff.

## Sizing process

1. Run representative workload with observability enabled.
2. Measure step latency percentiles and failure rate.
3. Increase parallelism gradually until latency/failures regress.
4. Set timeout above p99 steady-state latency plus margin.

## Saturation signals

- High timeout count (`PR104`) after raising parallelism.
- Increased step failure rate without upstream dependency changes.
- Long queueing delays for ready steps.

## Recommendation

Use separate profiles for dev/test/prod through config files and keep production limits explicit.
