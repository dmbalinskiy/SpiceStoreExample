﻿using SpiceStoreExample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceStoreExample.Utility
{
    public static class Consts
    {
        public const string DefaultFoodImage = "default_food.png";
        public const string ManagerUser = "Manager";
        public const string KitchenUser = "Kitchen";
        public const string FrontDeskUser = "FrontDesk";
        public const string CustomerEndUser = "Customer";

		public const string ShoppingCartCount = "ssCartCount"; 
		public const string CouponCode = "ssCouponCode";

		public const string statusSubmitted = "Submitted";
		public const string statusInProcess = "Being Prepared";
		public const string statusReady = "Ready for Pickup";
		public const string statusCompleted = "Completed";
		public const string statusCanceled = "Canceled";

		public const string PaymentStatusPending = "Pending";
		public const string PaymentStatusApproved = "Approved";
		public const string PaymentStatusRejected = "Rejected";


		public static string ConvertToRawHtml(string source)
		{
			char[] array = new char[source.Length];
			int arrayIndex = 0;
			bool inside = false;

			for (int i = 0; i < source.Length; i++)
			{
				char let = source[i];
				if (let == '<')
				{
					inside = true;
					continue;
				}
				if (let == '>')
				{
					inside = false;
					continue;
				}
				if (!inside)
				{
					array[arrayIndex] = let;
					arrayIndex++;
				}
			}
			return new string(array, 0, arrayIndex);
		}

		public static double DiscountedPrice(Coupon coupon, double totalOriginal)
		{
			double ret = totalOriginal;
			if(coupon != null && coupon.MinimumAmount <= totalOriginal)
			{
				if (Convert.ToInt32(coupon.CouponType) == (int)Coupon.ECouponType.Dollar)
				{
					ret = totalOriginal - coupon.Discount;
				}
				else if (Convert.ToInt32(coupon.CouponType) == (int)Coupon.ECouponType.Percent)
				{
					ret = totalOriginal - totalOriginal * coupon.Discount / 100;
				}
			}
			return Math.Round(ret, 2);
		}
	}
}
