# CodeBridge Advanced Board Runtime Roadmap

CodeBridge should expose the easy path first, but it must not become a thin serial bridge. The long-term runtime needs to let advanced users reach board-level capabilities that are normally written in C++: fast sampling, hardware timers, interrupts, buffered streaming, and memory-safe dashboards.

## Design Goals

- Keep the visual flow approachable for first-day users.
- Let advanced users configure board behavior explicitly.
- Move high-frequency work onto firmware-side services instead of pushing every sample through UI event loops.
- Bound memory by design with ring buffers, backpressure, and sampling policies.
- Reuse the same flow document across WinForms, Blazor, and MAUI.

## Runtime Layers

1. Board profile
   - Pin capabilities, ADC resolution, PWM channels, interrupt support, timer support, max safe sample rates.
   - Example: ESP32 DevKit exposes GPIO metadata, ADC pins, strapping-pin warnings, input-only pins, and PWM/timer limits.

2. Acquisition plan
   - Declarative config for sampling mode, interval, burst size, averaging, debounce, trigger condition, and buffer size.
   - The plan is compiled into board commands before runtime starts.

3. Firmware service
   - Runs timers, interrupts, ADC sampling, GPIO capture, PWM, servo, and streaming locally on the board.
   - Sends compact frames to the desktop instead of verbose command-per-sample messages.

4. Transport stream
   - Serial or WiFi transport with framed binary or compact JSON packets.
   - Supports sequence numbers, timestamps, dropped-frame counters, and health stats.

5. Dashboard buffer
   - Bounded ring buffer per channel.
   - UI consumes snapshots at visual refresh rate, not raw acquisition rate.
   - Backpressure policy: drop oldest, decimate, aggregate, or pause stream.

## Advanced Blocks

- `Sample Channel`: configure GPIO/ADC source, rate, mode, resolution, averaging, and buffer size.
- `Interrupt Input`: configure pin, edge, debounce, queue size, and callback flow.
- `Hardware Timer`: configure period, jitter tolerance, run mode, and linked trigger.
- `Stream To Dashboard`: bind channels to chart series with decimation and retention.
- `Rate Limiter`: cap downstream flow execution rate.
- `Window Aggregate`: min/max/avg/rms/count over a sliding sample window.
- `Pulse Counter`: count rising/falling edges in hardware-supported mode.
- `PWM Output`: frequency, duty, resolution, channel allocation.

## Flow Editor Requirements

- Simple mode hides advanced fields.
- Advanced mode shows sampling, buffering, timer, interrupt, and dashboard policies.
- Each block validates against the selected board profile.
- Warnings should be specific: input-only pin, unsafe boot pin, ADC-only pin, timer/channel conflict, requested sample rate too high.
- Runtime trace should include sample rate, dropped frames, buffer utilization, and transport latency.

## MVP Advanced Milestone

1. Add board profile metadata for sample-rate and interrupt capabilities.
2. Add `Sample Channel` and `Stream To Dashboard` block definitions.
3. Add bounded ring buffer primitives in `CodeBridge.Flow` or a new runtime namespace.
4. Add transport frame type for streamed samples.
5. Add WinForms dashboard preview with decimated chart refresh.
6. Add tests for buffer retention, dropped-frame accounting, and validation of unsupported pins/rates.

## Success Criteria

- Blink remains one-click simple.
- A power user can configure a fast ADC/GPIO stream without writing C++.
- The UI never stores unbounded samples.
- Dashboard rendering is decoupled from board sample frequency.
- Flow validation catches unsafe or impossible board configurations before runtime.
