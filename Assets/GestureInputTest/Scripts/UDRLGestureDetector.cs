using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.VR.WSA.Input;

namespace arcsin16.GestureInputTest
{
    public class UDRLGestureDetector : MonoBehaviour
    {

        public TextMesh GestureDebugText;
        public enum Direction
        {
            Neutral, Up, Down, Right, Left
        }

        public enum GestureEventType
        {
            DETECTING, DETECTED
        }

        private static readonly Dictionary<Direction, char> DirectionCodeDic = new Dictionary<Direction, char>()
        {
            {Direction.Up,    'U' },
            {Direction.Down,  'D' },
            {Direction.Right, 'R' },
            {Direction.Left,  'L' },
        };

        public class GestureEventArgs : EventArgs
        {
            public GestureEventType Type { get; set; }
            public Direction Direction { get; set; }
            public string Pattern { get; set; }
        }

        public static EventHandler<GestureEventArgs> GestureDetected;

        public float GestureThreshold = 0.05f;

        private bool notifyGesture;

        private Vector3 lastPos;
        private Direction currentDirection;

        private StringBuilder gesturePattern = new StringBuilder();
        private float accumulatedDx;
        private float accumulatedDy;

        void OnEnable()
        {
            InteractionManager.SourcePressed += OnSourcePressed;
            InteractionManager.SourceReleased += OnSourceReleased;
            InteractionManager.SourceUpdated += OnSourceUpdated;
            InteractionManager.SourceLost += OnSourceLost;
            InteractionManager.SourceDetected += OnSourceDetected;
        }



        void OnDisable()
        {
            InteractionManager.SourcePressed -= OnSourcePressed;
            InteractionManager.SourceReleased -= OnSourceReleased;
            InteractionManager.SourceUpdated -= OnSourceUpdated;
            InteractionManager.SourceLost -= OnSourceLost;
            InteractionManager.SourceDetected -= OnSourceDetected;
        }

        private void OnSourceDetected(InteractionSourceState state)
        {
            Vector3 pos;
            if (state.properties.location.TryGetPosition(out pos))
            {
                this.lastPos = pos;
            }

            this.notifyGesture = false;
            this.currentDirection = Direction.Neutral;
            this.accumulatedDx = 0;
            this.accumulatedDy = 0;
        }

        private void OnSourcePressed(InteractionSourceState state)
        {
            // タップ開始時にジェスチャー通知を有効化
            // ※タップ開始前から、ジェスチャーの追跡は行っている。
            if (!this.notifyGesture)
            {
                this.notifyGesture = true;
                gesturePattern.Remove(0, gesturePattern.Length);

                // タップし始めたタイミングで、事前の移動量を多少引き継ぐ
                this.accumulatedDx = Mathf.Min(this.accumulatedDx, GestureThreshold);
                this.accumulatedDy = Mathf.Min(this.accumulatedDy, GestureThreshold);
                this.accumulatedDx = Mathf.Max(this.accumulatedDx, -GestureThreshold);
                this.accumulatedDy = Mathf.Max(this.accumulatedDy, -GestureThreshold);
                this.accumulatedDx /= 2;
                this.accumulatedDy /= 2;
            }
        }

        private void OnSourceUpdated(InteractionSourceState state)
        {
            // ジェスチャーを更新
            TraceGesture(state);
        }

        private void OnSourceReleased(InteractionSourceState state)
        {
            // タップ終了時にジェスチャーを確定
            NotifyGestureDetected();
        }

        private void OnSourceLost(InteractionSourceState state)
        {
            // トラッキングロスト時にジェスチャーを確定
            NotifyGestureDetected();
        }

        private void TraceGesture(InteractionSourceState state)
        {
            // カメラの上方向をy軸、右方向をx軸とする
            var axisY = Camera.main.transform.up;
            var axisX = Camera.main.transform.right;

            // 手の位置を取得
            Vector3 pos;
            if (state.properties.location.TryGetPosition(out pos))
            {
                // 手の移動量
                var diff = pos - this.lastPos;
                this.lastPos = pos;

                // x,y軸方向の移動量を取得する
                float dx = Vector3.Dot(axisX, diff);
                float dy = Vector3.Dot(axisY, diff);

                // 誤差の蓄積で暴発しないように、dx, dyの大きい要素のみ加算する
                // TODO: これむしろダメかも
                if (Mathf.Abs(dx) > Mathf.Abs(dy))
                {
                    // 検知中の方向への加算は0に抑制する
                    if (this.currentDirection == Direction.Right && dx > 0)
                    {
                        this.accumulatedDx += 0;
                    }
                    else if (this.currentDirection == Direction.Left && dx < 0)
                    {
                        this.accumulatedDx += 0;
                    }
                    else
                    {
                        this.accumulatedDx += dx;
                    }
                }
                else
                {
                    // 検知中の方向への加算は0に抑制する
                    if (this.currentDirection == Direction.Up && dy > 0)
                    {
                        this.accumulatedDy += 0;
                    }
                    else if (this.currentDirection == Direction.Down && dy < 0)
                    {
                        this.accumulatedDy += 0;
                    }
                    else
                    {
                        this.accumulatedDy += dy;
                    }
                }

                // 蓄積した移動量が閾値以上になれば、ジェスチャーと判定
                // Right
                Direction dir = Direction.Neutral;
                if (this.accumulatedDx > GestureThreshold)
                {
                    dir = Direction.Right;
                }
                // Left
                else if (this.accumulatedDx < -GestureThreshold)
                {
                    dir = Direction.Left;
                }
                // Up
                else if (this.accumulatedDy > GestureThreshold)
                {
                    dir = Direction.Up;
                }
                // Down
                else if (this.accumulatedDy < -GestureThreshold)
                {
                    dir = Direction.Down;
                }

                if (dir != Direction.Neutral && this.currentDirection != dir)
                {
                    Debug.LogFormat("Gesture Detect {0}", dir);
                    if (this.GestureDebugText != null) this.GestureDebugText.text += string.Format("\r\n Detect Gesture: {0}", dir);
                    this.currentDirection = dir;
                    this.accumulatedDx = 0;
                    this.accumulatedDy = 0;

                    this.gesturePattern.Append(DirectionCodeDic[dir]);
                    if (this.notifyGesture)
                    {
                        GestureDetected(this, new GestureEventArgs()
                        {
                            Type = GestureEventType.DETECTING,
                            Direction = this.currentDirection,
                            Pattern = this.gesturePattern.ToString()
                        });
                    }
                }
            }
            else
            {
                Debug.Log("InteractionSourceLocation::TryGetPosition failed.");
                if (this.GestureDebugText != null) this.GestureDebugText.text += "\r\n InteractionSourceLocation::TryGetPosition failed";
            }
        }

        private void NotifyGestureDetected()
        {
            if (this.notifyGesture)
            {
                if (this.currentDirection != Direction.Neutral)
                {
                    GestureDetected(this, new GestureEventArgs()
                    {
                        Type = GestureEventType.DETECTED,
                        Direction = this.currentDirection,
                        Pattern = this.gesturePattern.ToString()
                    });
                }

                // Clear
                this.notifyGesture = false;
                this.currentDirection = Direction.Neutral;
                this.accumulatedDx = 0;
                this.accumulatedDy = 0;
            }
        }
    }
}
