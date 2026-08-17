from pysensationcore import *

# PotteryHaptics用の最小Sensation Block。
# 単一の集束点(point)を、可変の強さ(intensity)で出力するだけのシンプルな構成。
# 毎フレームUnity側(HapticDeviceService)からpoint/intensityを更新して使う想定。
#
# 既存のCircleSensation等(円軌道を自動で描く)とは違い、軌道の計算は一切行わない。
# 位置制御はすべてFingerTracker(Unity側)の指位置追従ロジックに委ねる。

potteryFocalPointBlock = defineBlock("PotteryFocalPoint")
defineInputs(potteryFocalPointBlock, "point", "intensity")

defineBlockInputDefaultValue(potteryFocalPointBlock.intensity, (0.0, 0.0, 0.0))
setMetaData(potteryFocalPointBlock.intensity, "Type", "Scalar")

def emitFocalPoint(inputs):
    point = inputs[0]
    intensity = inputs[1][0]
    return (point[0], point[1], point[2], intensity)

defineOutputs(potteryFocalPointBlock, "out")
defineBlockOutputBehaviour(potteryFocalPointBlock.out, emitFocalPoint)
setMetaData(potteryFocalPointBlock.out, "Sensation-Producing", True)

attachDocumentation(potteryFocalPointBlock,
                    """
                    単一の集束点をintensityの強さで出力する最小構成のSensation。
                    point: (x, y, z) - エミッタ空間での位置(メートル単位)
                    intensity: 0.0〜1.0を想定。0でほぼ無感、1で最大出力。
                    """)
