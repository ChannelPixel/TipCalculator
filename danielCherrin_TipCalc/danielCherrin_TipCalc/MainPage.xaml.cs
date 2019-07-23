using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace danielCherrin_TipCalc
{
	public partial class MainPage : ContentPage
	{
        float billAmount = 0f;
        float tipAmount = 0f;
        float totalAmount = 0f;
        float foreignBillAmount = 0f;
        float foreignTipAmount = 0f;
        float foreignTotalAmount = 0f;
        float foreignConversionRate = 0f;
        MoneyEntryStates currentState = MoneyEntryStates.Start;


        enum MoneyEntryStates
        {
            Start = 0,
            TypingWholeNumber = 1,
            TypingDecimalPoint = 2,
            TypingFirstDecimal = 3,
            TypingSecondDecimal = 4,
            Finished = 5,
            DoNothing = 6
        }

        enum MoneyEntryEvents
        {
            Pressed0 = 0,
            Pressed1to9 = 1,
            PressedPeriod = 2,
            PressedClear = 3
        }

		public MainPage()
		{
			InitializeComponent();
            cmbbx_foreignCurr.SelectedItem = cmbbx_foreignCurr.Items[0];
        }

        void UpdateUI()
        {
            billAmountLabel.Text = "$" + billAmount.ToString();
            tipAmountLabel.Text = "$" + tipAmount.ToString();
            totalAmountLabel.Text = "$" + totalAmount.ToString();
            foreignBillAmountLabel.Text = "$" + foreignBillAmount.ToString();
            foreignTipAmountLabel.Text = "$" + foreignTipAmount.ToString();
            foreignTotalAmountLabel.Text = "$" + foreignTotalAmount.ToString();
        }

        void CalcAmounts()
        {
            tipAmount = billAmount * (float.Parse(percentageSlider.Value.ToString())/100);
            totalAmount = billAmount + tipAmount;

            foreignBillAmount = billAmount * foreignConversionRate;
            foreignBillAmount = tipAmount * foreignConversionRate;
            foreignBillAmount = totalAmount * foreignConversionRate;
        }

        private void percentageSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            CalcAmounts();
            UpdateUI();
        }

        private void Button_Pressed(object sender, EventArgs e)
        {
            Button sButton = sender as Button;
            Debug.WriteLine(sButton.Text);

            MoneyEntryStates[,] stateTransitionTable = new MoneyEntryStates[,]
                {
                    { MoneyEntryStates.DoNothing,
                      MoneyEntryStates.TypingWholeNumber,
                      MoneyEntryStates.TypingFirstDecimal,
                      MoneyEntryStates.TypingSecondDecimal,
                      MoneyEntryStates.Finished,
                      MoneyEntryStates.DoNothing }

                    ,{ MoneyEntryStates.TypingWholeNumber,
                        MoneyEntryStates.TypingWholeNumber,
                        MoneyEntryStates.TypingFirstDecimal,
                        MoneyEntryStates.TypingSecondDecimal,
                        MoneyEntryStates.Finished,
                        MoneyEntryStates.DoNothing }

                    ,{ MoneyEntryStates.TypingDecimalPoint,
                        MoneyEntryStates.TypingDecimalPoint,
                        MoneyEntryStates.DoNothing,
                        MoneyEntryStates.DoNothing,
                        MoneyEntryStates.DoNothing,
                        MoneyEntryStates.DoNothing }

                    ,{ MoneyEntryStates.Start,
                        MoneyEntryStates.Start,
                        MoneyEntryStates.Start,
                        MoneyEntryStates.Start,
                        MoneyEntryStates.Start,
                        MoneyEntryStates.Start },

                };

            MoneyEntryEvents currentEvent;

            switch (sButton.Text)
            {
                case "0":
                    currentEvent = MoneyEntryEvents.Pressed0;
                    break;
                case ".":
                    currentEvent = MoneyEntryEvents.PressedPeriod;
                    break;
                case "c":
                case "C":
                    currentEvent = MoneyEntryEvents.PressedClear;
                    break;
                default:
                    currentEvent = MoneyEntryEvents.Pressed1to9;
                    break;
            }

            MoneyEntryStates newCurrentState = stateTransitionTable[(int)currentEvent, (int)currentState];

            if (newCurrentState != MoneyEntryStates.DoNothing)
            {
                currentState = newCurrentState;

                if (currentState == MoneyEntryStates.Start)
                {
                    billAmount = 0f;
                }
                else if (currentState == MoneyEntryStates.TypingWholeNumber)
                {
                    billAmount *= 10;

                    billAmount += int.Parse(sButton.Text);
                }
                else if (currentState == MoneyEntryStates.TypingFirstDecimal)
                {
                    billAmount += 0.1f * int.Parse(sButton.Text);
                }
                else if (currentState == MoneyEntryStates.TypingSecondDecimal)
                {
                    billAmount += 0.01f * int.Parse(sButton.Text);
                }

                CalcAmounts();
                UpdateUI();
            }
        }

        string ConstructAPILink(string currAbrev)
        {
            return "https://free.currconv.com/api/v7/convert?q=AUD_" + currAbrev + "&compact=ultra&apiKey=f074ae5bff84a10186b2";
        }

        public async Task<string> GetValueFromAPIAsync()
        {
            var client = new HttpClient();

            try
            {
                var response = await client.GetAsync(ConstructAPILink((string)cmbbx_foreignCurr.SelectedItem));

                if (response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.Content == null)
                {
                    await DisplayAlert("Error", string.Format("Response contained status code:    {0}", response.StatusCode), "OK");
                    return "ERROR";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var indexStart = responseString.LastIndexOf(":");
                var substring = responseString.Substring(indexStart);
                var value = substring.Substring(1, substring.Length - 3);

                return value;
            }
            catch (HttpRequestException exc)
            {
                await DisplayAlert("Error", string.Format("Response contained status code:    {0}", exc.Message), "OK");
                return "ERROR";
            }
        }

        private async void cmbbx_foreignCurr_SelectedIndexChanged(object sender, EventArgs e)
        {
            string rateFromAPI = await GetValueFromAPIAsync();
            try
            {
                Debug.WriteLine(rateFromAPI);
                foreignConversionRate = float.Parse(rateFromAPI);
            }
            catch
            {
                await DisplayAlert("ERROR", "Value could not be fetched from the api", "OK");
            }
            CalcAmounts();
        }
    }
}
