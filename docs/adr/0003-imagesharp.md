# ADR 0003: ImageSharp image processing

Status: Accepted

ImageSharp 3.x handles PNG, JPEG, WebP and BMP processing without a license
key. Small in-house header readers perform inspection, and Windows' built-in
icon decoder selects the largest ICO input frame. A small writer produces
multi-size ICO output. This keeps all V1 formats while avoiding paid services.
A future proprietary distribution must still re-evaluate the Six Labors Split
License before publication.
