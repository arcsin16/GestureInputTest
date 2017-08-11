using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace arcsin16.GestureInputTest
{
    public class IPInputBehaviour : MonoBehaviour
    {
        public TextMesh IpText;
        public AudioClip upSound;
        public AudioClip downSound;
        public AudioClip rightSound;
        public AudioClip leftSound;
        private string ipText;
        private static readonly string PROMPT = "SharingServer IP:\r\n";
        private static readonly Dictionary<string, KeyCode> GesturePatternDic = new Dictionary<string, KeyCode>()
    {
        {"UL", KeyCode.Alpha1 },
        {"U",  KeyCode.Alpha2 },
        {"UR", KeyCode.Alpha3 },
        {"LU", KeyCode.Alpha4 },
        {"L",  KeyCode.Alpha5 },
        {"LD", KeyCode.Alpha6 },
        {"RU", KeyCode.Alpha7 },
        {"R",  KeyCode.Alpha8 },
        {"RD", KeyCode.Alpha9 },
        {"DL", KeyCode.Delete },
        {"D",  KeyCode.Alpha0 },
        {"DR", KeyCode.Period },
    };

        private Dictionary<UDRLGestureDetector.Direction, Func<AudioClip>> AudioClipDic;

        void OnEnable()
        {
            UDRLGestureDetector.GestureDetected += GestureDetected;
        }

        void OnDisable()
        {
            UDRLGestureDetector.GestureDetected -= GestureDetected;
        }

        void Start()
        {
            this.AudioClipDic = new Dictionary<UDRLGestureDetector.Direction, Func<AudioClip>>()
            {
                { UDRLGestureDetector.Direction.Up,    ()=> this.upSound},
                { UDRLGestureDetector.Direction.Down,  ()=> this.downSound},
                { UDRLGestureDetector.Direction.Right, ()=> this.rightSound},
                { UDRLGestureDetector.Direction.Left,  ()=> this.leftSound},
            };

            this.ipText = string.Empty;
        }

        void GestureDetected(object sender, UDRLGestureDetector.GestureEventArgs args)
        {
            // ジェスチャー中：現在選択中の入力を表示する
            if (args.Type == UDRLGestureDetector.GestureEventType.DETECTING)
            {
                KeyCode keyCode = GetKeyCode(args);
                // 選択中の表示を更新
                if (!IsValidInput(this.ipText, keyCode))
                {
                    IpText.text = PROMPT + this.ipText + "[]";
                }
                else
                {
                    if (keyCode == KeyCode.Delete)
                    {
                        IpText.text = PROMPT + this.ipText + "[DEL]";
                    }
                    else
                    {
                        IpText.text = PROMPT + this.ipText + "[" + (char)keyCode + "]";
                    }
                }

                // TODO: 現在認識しているジェスチャーを視覚的に表示する

                // ジェスチャーが認識されたタイミングを、効果音で通知する
                AudioClip clip = GetAudioClipOrNull(args);
                if (clip != null)
                    AudioSource.PlayClipAtPoint(clip, this.transform.position, 0.8f);

            }

            // ジェスチャー確定：入力を確定する
            else if (args.Type == UDRLGestureDetector.GestureEventType.DETECTED)
            {
                KeyCode keyCode = GetKeyCode(args);
                if (!IsValidInput(this.ipText, keyCode))
                {
                    IpText.text = PROMPT + this.ipText;
                    return;
                }
                // 削除処理
                if (keyCode == KeyCode.Delete)
                {
                    // 直前の文字が.の場合２文字消す
                    if (this.ipText.EndsWith(".") && this.ipText.Length > 1)
                    {
                        this.ipText = this.ipText.Remove(this.ipText.Length - 2);
                    }
                    // 通常は１文字
                    else if (this.ipText.Length > 0)
                    {
                        this.ipText = this.ipText.Remove(this.ipText.Length - 1);
                    }
                }
                else if (keyCode == KeyCode.Period)
                {
                    var blocks = this.ipText.Split('.');
                    if (blocks.Length == 4)
                    {
                        // IP 入力完了
                        this.InputCompleted();
                    }
                    else
                    {
                        this.ipText += '.';
                    }
                }
                // 文字入力
                else
                {
                    this.ipText += (char)keyCode;

                    var blocks = this.ipText.Split('.');
                    var editingBlock = blocks.LastOrDefault() ?? string.Empty;
                    // . を自動保管
                    if (editingBlock.Length == 3)
                    {
                        if (blocks.Length != 4)
                        {
                            this.ipText += ".";
                        }
                        else
                        {
                            this.InputCompleted();
                        }
                    }
                }

                IpText.text = PROMPT + this.ipText;
            }
        }

        private void InputCompleted()
        {
            // TODO: SharingStageのServeAddressを設定して、サーバに接続する
            if (this.upSound != null)
                AudioSource.PlayClipAtPoint(this.upSound, this.transform.position, 0.8f);
            if (this.rightSound != null)
                AudioSource.PlayClipAtPoint(this.rightSound, this.transform.position, 0.8f);
            if (this.leftSound != null)
                AudioSource.PlayClipAtPoint(this.leftSound, this.transform.position, 0.8f);
            if (this.downSound != null)
                AudioSource.PlayClipAtPoint(this.downSound, this.transform.position, 0.8f);
        }

        /// <summary>
        /// 入力コードのバリデーション
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool IsValidInput(string ip, KeyCode input)
        {
            if (input == KeyCode.None)
                return false;

            var editingBlock = ip.Split('.').LastOrDefault() ?? string.Empty;

            // 空白時のDelete
            if (input == KeyCode.Delete && ip.Length == 0)
                return false;

            // . の連続
            if (input == KeyCode.Period && editingBlock.Length == 0)
                return false;


            // 数値入力
            if (input != KeyCode.Period && input != KeyCode.Delete)
            {
                // 0～255 の範囲外
                Debug.LogFormat("Input {0}", (char)input);
                int val = int.Parse(editingBlock + (char)input);
                if (val < 0 || val > 255)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// ジェスチャーに対応するKeyCodeを取得する
        /// 対応するものがなければKeyCode.None
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static KeyCode GetKeyCode(UDRLGestureDetector.GestureEventArgs args)
        {
            KeyCode value;
            return GesturePatternDic.TryGetValue(args.Pattern, out value) ? value : KeyCode.None;
        }

        /// <summary>
        /// ジェスチャーに対応するAudioClipを取得する
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private AudioClip GetAudioClipOrNull(UDRLGestureDetector.GestureEventArgs args)
        {
            Func<AudioClip> clipFunc;
            return AudioClipDic.TryGetValue(args.Direction, out clipFunc) ? clipFunc() : null;
        }
    }
}
